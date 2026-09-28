using FieldFlow.Api.IntegrationTests.Infrastructure;
using FieldFlow.Application.Abstractions;
using FieldFlow.Domain.Assets;
using FieldFlow.Domain.Catalog;
using FieldFlow.Domain.Common;
using FieldFlow.Domain.Customers;
using FieldFlow.Domain.Interfaces;
using FieldFlow.Domain.Technicians;
using FieldFlow.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FieldFlow.Api.IntegrationTests.Persistence;

/// <summary>Contratos de persistência contra PostgreSQL real (Testcontainers).</summary>
[Collection(PostgresCollection.Name)]
public sealed class PersistenceTests : IAsyncLifetime
{
    private readonly FieldFlowApiFactory _factory;

    public PersistenceTests(PostgresFixture postgres)
        => _factory = FieldFlowApiFactory.WithIsolatedDatabase(postgres.ConnectionString);

    public Task InitializeAsync()
    {
        _ = _factory.Server; // força o boot (cria o schema)
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    [Fact]
    public async Task Every_BaseEntity_has_a_soft_delete_query_filter()
    {
        var missing = await _factory.WithDbContextAsync(ctx => Task.FromResult(
            ctx.Model.GetEntityTypes()
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType) && t.GetQueryFilter() is null)
                .Select(t => t.ClrType.Name)
                .ToList()));

        missing.Should().BeEmpty();
    }

    [Fact]
    public async Task Tables_are_created_in_fieldflow_schema_with_snake_case_names()
    {
        var tables = await _factory.WithDbContextAsync(ctx => ctx.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'fieldflow'")
            .ToListAsync());

        tables.Should().Contain(["customers", "assets", "technicians", "technician_skills", "service_types", "parts",
            "holidays", "users", "work_orders", "work_order_events", "work_order_time_entries"]);
    }

    [Fact]
    public async Task Delete_is_soft_and_filtered_out_of_queries()
    {
        var part = Part.Create("TST-001", "Peça de teste", 10m, 5).Value;

        await _factory.WithScopeAsync(async sp =>
        {
            await sp.GetRequiredService<IRepository<Part>>().AddAsync(part);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

        await _factory.WithScopeAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IRepository<Part>>();
            var loaded = await repository.GetByIdAsync(part.PartId);
            repository.Delete(loaded!);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

        var (visible, raw) = await _factory.WithDbContextAsync(async ctx => (
            await ctx.Parts.AnyAsync(p => p.PartId == part.PartId),
            await ctx.Parts.IgnoreQueryFilters().SingleAsync(p => p.PartId == part.PartId)));

        visible.Should().BeFalse();
        raw.IsDeleted.Should().BeTrue();
        raw.DeletedAt.Should().NotBeNull();
        raw.DeletedBy.Should().Be("System");
    }

    [Fact]
    public async Task Audit_columns_are_filled_on_insert_and_update()
    {
        var part = Part.Create("TST-002", "Peça auditada", 10m, 5).Value;
        await _factory.WithDbContextAsync(async ctx =>
        {
            ctx.Parts.Add(part);
            return await ctx.SaveChangesAsync();
        });

        await _factory.WithDbContextAsync(async ctx =>
        {
            var loaded = await ctx.Parts.SingleAsync(p => p.PartId == part.PartId);
            loaded.IncreaseStock(1);
            return await ctx.SaveChangesAsync();
        });

        var saved = await _factory.WithDbContextAsync(ctx => ctx.Parts.AsNoTracking().SingleAsync(p => p.PartId == part.PartId));
        saved.CreatedBy.Should().Be("System");
        saved.CreatedAt.Should().NotBe(default(DateTime));
        saved.UpdatedBy.Should().Be("System");
        saved.UpdatedAt.Should().NotBeNull();
        saved.StockQuantity.Should().Be(6);
    }

    [Fact]
    public async Task Work_order_aggregate_round_trips_with_children_and_timeline()
    {
        Guid workOrderId = default;

        await _factory.WithScopeAsync(async sp =>
        {
            var ctx = sp.GetRequiredService<FieldFlow.Infrastructure.Persistence.ApplicationDbContext>();
            var sla = await sp.GetRequiredService<IBusinessCalendarProvider>().GetSlaCalculatorAsync();
            var workOrders = sp.GetRequiredService<IWorkOrderRepository>();
            var now = DateTime.UtcNow;

            var address = Address.Create("Rua Teste", "1", "Campinas", "SP", "13000000").Value;
            var customer = Customer.Create("Cliente Teste", "11.222.333/0001-81", "teste@example.com", "1933334444", address).Value;
            var asset = Asset.Create(customer.CustomerId, "TST-01", "Modelo", "Fabricante", "SN1", null, null).Value;
            var serviceType = ServiceType.Create("Teste", 60, SlaPolicy.Create(48, 24, 8, 4).Value).Value;
            var technician = Technician.Create("Técnico Teste", WorkingHours.Standard).Value;
            technician.AddSkill(serviceType.ServiceTypeId);
            ctx.AddRange(customer, asset, serviceType, technician);

            var number = await workOrders.NextNumberAsync(now.Year);
            var wo = WorkOrder.Create(number, customer.CustomerId, asset, serviceType, Priority.High, "Teste", "tester", now.AddHours(-5)).Value;
            wo.Open(sla, "tester", now.AddHours(-5)).IsSuccess.Should().BeTrue();
            wo.Assign(technician, now.AddHours(-4), now.AddHours(-2), "tester", now.AddHours(-4.5)).IsSuccess.Should().BeTrue();
            wo.Start("tester", now.AddHours(-4)).IsSuccess.Should().BeTrue();
            wo.AddChecklistItem("Ok?", true, "tester", now.AddHours(-4));
            wo.AddTimeEntry(technician.TechnicianId, now.AddHours(-4), now.AddHours(-3), null, "tester", now.AddHours(-3)).IsSuccess.Should().BeTrue();
            await workOrders.AddAsync(wo);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            workOrderId = wo.WorkOrderId;
        });

        // Novo escopo: carrega, altera (adiciona filho a um agregado rastreado) e salva
        await _factory.WithScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IWorkOrderRepository>();
            var wo = await repo.GetWithDetailsAsync(workOrderId);
            wo!.AnswerChecklistItem(wo.ChecklistItems.Single().ChecklistItemId, "Sim", "tester", DateTime.UtcNow).IsSuccess.Should().BeTrue();
            wo.Complete("Resolvido", "tester", DateTime.UtcNow).IsSuccess.Should().BeTrue();
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

        var reloaded = await _factory.QueryScopeAsync(sp => sp.GetRequiredService<IWorkOrderRepository>().GetWithDetailsAsync(workOrderId));

        reloaded!.Status.Should().Be(WorkOrderStatus.Completed);
        reloaded.Number.Value.Should().StartWith("OS-");
        reloaded.TimeEntries.Should().HaveCount(1);
        reloaded.ChecklistItems.Single().IsAnswered.Should().BeTrue();
        reloaded.Events.Select(e => e.Type).Should().Contain([WorkOrderEventType.Created, WorkOrderEventType.Completed]);
        reloaded.Events.Should().HaveCount(8); // created, opened, assigned, started, checklist+, time entry, answered, completed
    }

    [Fact]
    public async Task Work_order_numbers_are_sequential()
    {
        var (first, second) = await _factory.QueryScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IWorkOrderRepository>();
            return (await repo.NextNumberAsync(2026), await repo.NextNumberAsync(2026));
        });

        long.Parse(second.Value[8..], System.Globalization.CultureInfo.InvariantCulture)
            .Should().Be(long.Parse(first.Value[8..], System.Globalization.CultureInfo.InvariantCulture) + 1);
    }
}
