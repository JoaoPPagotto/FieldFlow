using FieldFlow.Api.IntegrationTests.Infrastructure;
using FieldFlow.Domain.Assets;
using FieldFlow.Domain.WorkOrders;
using FieldFlow.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FieldFlow.Api.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class DemoSeedTests : IAsyncLifetime
{
    private readonly FieldFlowApiFactory _factory;

    public DemoSeedTests(PostgresFixture postgres)
        => _factory = FieldFlowApiFactory.WithIsolatedDatabase(postgres.ConnectionString, seedDemoData: true);

    public Task InitializeAsync()
    {
        _ = _factory.Server; // boot: cria schema + seed
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    [Fact]
    public async Task Seed_creates_the_demo_data_set()
    {
        var counts = await _factory.WithDbContextAsync(async ctx => new
        {
            Customers = await ctx.Customers.CountAsync(),
            Assets = await ctx.Assets.CountAsync(),
            Decommissioned = await ctx.Assets.CountAsync(a => a.Status == AssetStatus.Decommissioned),
            Technicians = await ctx.Technicians.CountAsync(),
            ServiceTypes = await ctx.ServiceTypes.CountAsync(),
            Holidays = await ctx.Holidays.CountAsync(),
            WorkOrders = await ctx.WorkOrders.CountAsync()
        });

        counts.Customers.Should().Be(3);
        counts.Assets.Should().Be(12);
        counts.Decommissioned.Should().Be(1);
        counts.Technicians.Should().Be(5);
        counts.ServiceTypes.Should().Be(5);
        counts.Holidays.Should().BeGreaterThan(10);
        counts.WorkOrders.Should().Be(DemoDataSeeder.WorkOrderCount);
    }

    [Fact]
    public async Task Seeded_work_orders_cover_every_status_and_have_a_timeline()
    {
        var byStatus = await _factory.WithDbContextAsync(ctx => ctx.WorkOrders
            .GroupBy(w => w.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count));

        byStatus.Keys.Should().BeEquivalentTo(Enum.GetValues<WorkOrderStatus>());

        var withoutEvents = await _factory.WithDbContextAsync(ctx =>
            ctx.WorkOrders.CountAsync(w => !ctx.WorkOrderEvents.Any(e => e.WorkOrderId == w.WorkOrderId)));
        withoutEvents.Should().Be(0);
    }

    [Fact]
    public async Task Seed_is_idempotent()
    {
        await _factory.WithScopeAsync(sp => sp.GetRequiredService<DemoDataSeeder>().SeedAsync());

        var total = await _factory.WithDbContextAsync(ctx => ctx.WorkOrders.CountAsync());
        total.Should().Be(DemoDataSeeder.WorkOrderCount);
    }
}
