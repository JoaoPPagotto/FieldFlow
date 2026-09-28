using System.Linq.Expressions;
using FieldFlow.Domain.Assets;
using FieldFlow.Domain.Calendar;
using FieldFlow.Domain.Catalog;
using FieldFlow.Domain.Common;
using FieldFlow.Domain.Customers;
using FieldFlow.Domain.Technicians;
using FieldFlow.Domain.Users;
using FieldFlow.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FieldFlow.Infrastructure.Persistence;

/// <summary>
/// Fonte única de verdade do mapeamento. Cada entidade tem sua <c>IEntityTypeConfiguration</c>
/// em <c>Persistence/Configurations</c>; nada de <c>[Table]</c>/<c>[Column]</c> nas entidades.
/// </summary>
public partial class ApplicationDbContext : DbContext
{
    public const string Schema = "fieldflow";
    public const string WorkOrderNumberSequence = "work_order_number_seq";

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public virtual DbSet<Customer> Customers => Set<Customer>();
    public virtual DbSet<Asset> Assets => Set<Asset>();
    public virtual DbSet<Technician> Technicians => Set<Technician>();
    public virtual DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public virtual DbSet<Part> Parts => Set<Part>();
    public virtual DbSet<Holiday> Holidays => Set<Holiday>();
    public virtual DbSet<User> Users => Set<User>();
    public virtual DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public virtual DbSet<WorkOrderEvent> WorkOrderEvents => Set<WorkOrderEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.HasSequence<long>(WorkOrderNumberSequence).StartsAt(1).IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        OnModelCreatingPartial(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;
            if (entityType.IsOwned()) continue;

            // Eventos de domínio vivem só em memória.
            if (typeof(AggregateRoot).IsAssignableFrom(clrType))
                modelBuilder.Entity(clrType).Ignore(nameof(AggregateRoot.DomainEvents));

            // Soft delete global: toda entidade que herda BaseEntity ganha
            // HasQueryFilter(e => e.IsDeleted == false) automaticamente.
            if (typeof(BaseEntity).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var prop = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var condition = Expression.Equal(prop, Expression.Constant(false));
                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
