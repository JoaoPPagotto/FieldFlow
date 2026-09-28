using FieldFlow.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldFlow.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.CustomerId).HasName("customers_pkey");
        builder.Property(c => c.CustomerId).ValueGeneratedNever();

        builder.Property(c => c.Name).HasMaxLength(Customer.NameMaxLength).IsRequired();
        builder.Property(c => c.Document)
            .HasConversion(cnpj => cnpj.Value, value => Cnpj.Create(value).Value)
            .HasMaxLength(14)
            .IsRequired();
        builder.Property(c => c.Email).HasMaxLength(254).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(11).IsRequired();

        builder.ComplexProperty(c => c.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("address_street").HasMaxLength(200).IsRequired();
            address.Property(a => a.Number).HasColumnName("address_number").HasMaxLength(20).IsRequired();
            address.Property(a => a.City).HasColumnName("address_city").HasMaxLength(100).IsRequired();
            address.Property(a => a.State).HasColumnName("address_state").HasMaxLength(2).IsFixedLength().IsRequired();
            address.Property(a => a.ZipCode).HasColumnName("address_zip_code").HasMaxLength(8).IsFixedLength().IsRequired();
        });

        builder.HasMany(c => c.Assets)
            .WithOne()
            .HasForeignKey(a => a.CustomerId)
            .HasConstraintName("fk_assets_customer")
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Assets).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => c.Document).IsUnique().HasFilter(ConfigurationExtensions.NotDeleted)
            .HasDatabaseName("ux_customers_document");
        builder.HasIndex(c => c.Name).HasDatabaseName("ix_customers_name");

        builder.ConfigureAudit();
    }
}
