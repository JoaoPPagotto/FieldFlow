using FieldFlow.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldFlow.Infrastructure.Persistence.Configurations;

internal sealed class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.ToTable("parts");
        builder.HasKey(p => p.PartId).HasName("parts_pkey");
        builder.Property(p => p.PartId).ValueGeneratedNever();

        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.UnitPrice).HasPrecision(12, 2);
        builder.Property(p => p.StockQuantity).IsConcurrencyToken(); // baixa de estoque concorrente

        builder.HasIndex(p => p.Code).IsUnique().HasFilter(ConfigurationExtensions.NotDeleted)
            .HasDatabaseName("ux_parts_code");

        builder.ConfigureAudit();
    }
}
