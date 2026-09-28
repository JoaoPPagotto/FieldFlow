using FieldFlow.Domain.Assets;
using FieldFlow.Domain.Catalog;
using FieldFlow.Domain.Customers;
using FieldFlow.Domain.Technicians;
using FieldFlow.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldFlow.Infrastructure.Persistence.Configurations;

internal sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("work_orders");
        builder.HasKey(w => w.WorkOrderId).HasName("work_orders_pkey");
        builder.Property(w => w.WorkOrderId).ValueGeneratedNever();

        // Concorrência otimista via coluna de sistema xmin do PostgreSQL
        builder.Property<uint>("Version").IsRowVersion();

        builder.Property(w => w.Number)
            .HasConversion(n => n.Value, value => WorkOrderNumber.Parse(value).Value)
            .HasMaxLength(14)
            .IsRequired();
        builder.HasIndex(w => w.Number).IsUnique().HasDatabaseName("ux_work_orders_number");

        builder.Property(w => w.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(WorkOrder.DescriptionMaxLength).IsRequired();
        builder.Property(w => w.HoldReason).HasMaxLength(WorkOrder.ReasonMaxLength);
        builder.Property(w => w.CancellationReason).HasMaxLength(WorkOrder.ReasonMaxLength);
        builder.Property(w => w.ResolutionNotes).HasMaxLength(WorkOrder.DescriptionMaxLength);

        builder.Property(w => w.OpenedAt).HasPrecision(0);
        builder.Property(w => w.DueAt).HasPrecision(0);
        builder.Property(w => w.StartedAt).HasPrecision(0);
        builder.Property(w => w.ClosedAt).HasPrecision(0);
        builder.Property(w => w.ScheduledStart).HasPrecision(0);
        builder.Property(w => w.ScheduledEnd).HasPrecision(0);
        builder.Property(w => w.HoldStartedAt).HasPrecision(0);

        // Referências entre agregados: só por Id + FK
        builder.HasOne<Customer>().WithMany().HasForeignKey(w => w.CustomerId)
            .HasConstraintName("fk_work_orders_customer").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Asset>().WithMany().HasForeignKey(w => w.AssetId)
            .HasConstraintName("fk_work_orders_asset").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ServiceType>().WithMany().HasForeignKey(w => w.ServiceTypeId)
            .HasConstraintName("fk_work_orders_service_type").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Technician>().WithMany().HasForeignKey(w => w.AssignedTechnicianId)
            .HasConstraintName("fk_work_orders_technician").OnDelete(DeleteBehavior.Restrict);

        // Filhos do agregado (acesso pelo backing field)
        builder.HasMany(w => w.TimeEntries).WithOne().HasForeignKey(t => t.WorkOrderId)
            .HasConstraintName("fk_time_entries_work_order").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(w => w.UsedParts).WithOne().HasForeignKey(p => p.WorkOrderId)
            .HasConstraintName("fk_used_parts_work_order").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(w => w.ChecklistItems).WithOne().HasForeignKey(c => c.WorkOrderId)
            .HasConstraintName("fk_checklist_items_work_order").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(w => w.Attachments).WithOne().HasForeignKey(a => a.WorkOrderId)
            .HasConstraintName("fk_attachments_work_order").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(w => w.Events).WithOne().HasForeignKey(e => e.WorkOrderId)
            .HasConstraintName("fk_events_work_order").OnDelete(DeleteBehavior.Cascade);

        foreach (var navigation in new[]
                 {
                     nameof(WorkOrder.TimeEntries), nameof(WorkOrder.UsedParts), nameof(WorkOrder.ChecklistItems),
                     nameof(WorkOrder.Attachments), nameof(WorkOrder.Events)
                 })
        {
            builder.Navigation(navigation).UsePropertyAccessMode(PropertyAccessMode.Field);
        }

        // Índices para listagem, agenda e dashboard
        builder.HasIndex(w => w.Status).HasDatabaseName("ix_work_orders_status");
        builder.HasIndex(w => w.CustomerId).HasDatabaseName("ix_work_orders_customer");
        builder.HasIndex(w => w.AssetId).HasDatabaseName("ix_work_orders_asset");
        builder.HasIndex(w => new { w.AssignedTechnicianId, w.ScheduledStart }).HasDatabaseName("ix_work_orders_technician_schedule");
        builder.HasIndex(w => w.DueAt).HasDatabaseName("ix_work_orders_due_at");

        builder.ConfigureAudit();
    }
}

internal sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("work_order_time_entries");
        builder.HasKey(t => t.TimeEntryId).HasName("work_order_time_entries_pkey");
        builder.Property(t => t.TimeEntryId).ValueGeneratedNever();

        builder.Property(t => t.StartedAt).HasPrecision(0);
        builder.Property(t => t.EndedAt).HasPrecision(0);
        builder.Property(t => t.Notes).HasMaxLength(1000);

        builder.HasOne<Technician>().WithMany().HasForeignKey(t => t.TechnicianId)
            .HasConstraintName("fk_time_entries_technician").OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => new { t.TechnicianId, t.StartedAt }).HasDatabaseName("ix_time_entries_technician_started");

        builder.ConfigureAudit();
    }
}

internal sealed class UsedPartConfiguration : IEntityTypeConfiguration<UsedPart>
{
    public void Configure(EntityTypeBuilder<UsedPart> builder)
    {
        builder.ToTable("work_order_used_parts");
        builder.HasKey(p => p.UsedPartId).HasName("work_order_used_parts_pkey");
        builder.Property(p => p.UsedPartId).ValueGeneratedNever();

        builder.Property(p => p.PartCode).HasMaxLength(30).IsRequired();
        builder.Property(p => p.UnitPrice).HasPrecision(12, 2);

        builder.HasOne<Part>().WithMany().HasForeignKey(p => p.PartId)
            .HasConstraintName("fk_used_parts_part").OnDelete(DeleteBehavior.Restrict);

        builder.ConfigureAudit();
    }
}

internal sealed class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("work_order_checklist_items");
        builder.HasKey(c => c.ChecklistItemId).HasName("work_order_checklist_items_pkey");
        builder.Property(c => c.ChecklistItemId).ValueGeneratedNever();

        builder.Property(c => c.Question).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Answer).HasMaxLength(1000);
        builder.Property(c => c.AnsweredBy).HasMaxLength(120);
        builder.Property(c => c.AnsweredAt).HasPrecision(0);
        builder.Property(c => c.Order).HasColumnName("display_order");

        builder.ConfigureAudit();
    }
}

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("work_order_attachments");
        builder.HasKey(a => a.AttachmentId).HasName("work_order_attachments_pkey");
        builder.Property(a => a.AttachmentId).ValueGeneratedNever();

        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(a => a.UploadedBy).HasMaxLength(120).IsRequired();
        builder.Property(a => a.UploadedAt).HasPrecision(0);

        builder.ConfigureAudit();
    }
}

internal sealed class WorkOrderEventConfiguration : IEntityTypeConfiguration<WorkOrderEvent>
{
    public void Configure(EntityTypeBuilder<WorkOrderEvent> builder)
    {
        builder.ToTable("work_order_events");
        builder.HasKey(e => e.WorkOrderEventId).HasName("work_order_events_pkey");
        builder.Property(e => e.WorkOrderEventId).ValueGeneratedNever();

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Actor).HasMaxLength(120).IsRequired();
        builder.Property(e => e.OccurredAt).HasPrecision(3);
        builder.Property(e => e.Payload).HasColumnType("jsonb");

        builder.HasIndex(e => new { e.WorkOrderId, e.OccurredAt }).HasDatabaseName("ix_work_order_events_timeline");
    }
}
