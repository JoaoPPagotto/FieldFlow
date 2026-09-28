using FieldFlow.Domain.Calendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldFlow.Infrastructure.Persistence.Configurations;

internal sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("holidays");
        builder.HasKey(h => h.HolidayId).HasName("holidays_pkey");
        builder.Property(h => h.HolidayId).ValueGeneratedNever();

        builder.Property(h => h.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(h => h.Date).IsUnique().HasFilter(ConfigurationExtensions.NotDeleted)
            .HasDatabaseName("ux_holidays_date");

        builder.ConfigureAudit();
    }
}
