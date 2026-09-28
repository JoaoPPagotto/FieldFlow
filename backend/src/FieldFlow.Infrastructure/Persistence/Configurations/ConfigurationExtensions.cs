using FieldFlow.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldFlow.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    /// <summary>Filtro SQL para índices únicos que ignoram registros soft-deleted.</summary>
    public const string NotDeleted = "is_deleted = false";

    /// <summary>Colunas de auditoria padrão (mesmos tipos/precisões em todas as tabelas).</summary>
    public static EntityTypeBuilder<T> ConfigureAudit<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("now()");
        builder.Property(e => e.CreatedBy).HasMaxLength(120);
        builder.Property(e => e.UpdatedAt).HasPrecision(0);
        builder.Property(e => e.UpdatedBy).HasMaxLength(120);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).HasPrecision(0);
        builder.Property(e => e.DeletedBy).HasMaxLength(120);
        return builder;
    }
}
