using FieldFlow.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldFlow.Infrastructure.Persistence.Configurations;

internal sealed class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.ToTable("service_types");
        builder.HasKey(s => s.ServiceTypeId).HasName("service_types_pkey");
        builder.Property(s => s.ServiceTypeId).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(ServiceType.NameMaxLength).IsRequired();
        builder.Property(s => s.DefaultDurationMinutes).IsRequired();

        builder.ComplexProperty(s => s.Sla, sla =>
        {
            sla.Property(p => p.LowHours).HasColumnName("sla_low_hours");
            sla.Property(p => p.MediumHours).HasColumnName("sla_medium_hours");
            sla.Property(p => p.HighHours).HasColumnName("sla_high_hours");
            sla.Property(p => p.CriticalHours).HasColumnName("sla_critical_hours");
        });

        builder.HasIndex(s => s.Name).IsUnique().HasFilter(ConfigurationExtensions.NotDeleted)
            .HasDatabaseName("ux_service_types_name");

        builder.ConfigureAudit();
    }
}
