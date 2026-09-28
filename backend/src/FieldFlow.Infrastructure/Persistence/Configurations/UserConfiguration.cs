using FieldFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldFlow.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.UserId).HasName("users_pkey");
        builder.Property(u => u.UserId).ValueGeneratedNever();

        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(150).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique().HasFilter(ConfigurationExtensions.NotDeleted)
            .HasDatabaseName("ux_users_email");

        builder.ConfigureAudit();
    }
}
