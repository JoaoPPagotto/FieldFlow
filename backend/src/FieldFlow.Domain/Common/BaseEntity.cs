namespace FieldFlow.Domain.Common;

/// <summary>
/// Base de toda entidade persistida: contém APENAS as colunas de auditoria padrão.
/// A chave primária fica em cada entidade (<c>{Entity}Id</c>).
/// <para>
/// O preenchimento é automático (interceptor de SaveChanges na Infrastructure) e o soft delete
/// é aplicado globalmente por query filter no <c>ApplicationDbContext</c> — nunca há hard delete.
/// </para>
/// </summary>
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    public void SetCreated(string createdBy, DateTime createdAtUtc)
    {
        CreatedBy = createdBy;
        CreatedAt = createdAtUtc;
    }

    public void SetUpdated(string updatedBy, DateTime updatedAtUtc)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAtUtc;
    }

    public void MarkDeleted(string? deletedBy, DateTime deletedAtUtc)
    {
        IsDeleted = true;
        DeletedAt = deletedAtUtc;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
