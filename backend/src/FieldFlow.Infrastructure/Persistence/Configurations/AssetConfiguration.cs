using FieldFlow.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldFlow.Infrastructure.Persistence.Configurations;

internal sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets");
        builder.HasKey(a => a.AssetId).HasName("assets_pkey");
        builder.Property(a => a.AssetId).ValueGeneratedNever();

        builder.Property(a => a.Tag).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Model).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Manufacturer).HasMaxLength(100).IsRequired();
        builder.Property(a => a.SerialNumber).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Tag única por cliente (entre os não excluídos)
        builder.HasIndex(a => new { a.CustomerId, a.Tag }).IsUnique().HasFilter(ConfigurationExtensions.NotDeleted)
            .HasDatabaseName("ux_assets_customer_tag");

        builder.ConfigureAudit();
    }
}
