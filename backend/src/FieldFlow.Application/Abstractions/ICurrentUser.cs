namespace FieldFlow.Application.Abstractions;

/// <summary>
/// Usuário da requisição atual, resolvido das claims do JWT (Fase 2).
/// Usado pela auditoria (created_by/updated_by/deleted_by) e pela autorização por recurso.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }

    /// <summary>Identificador gravado nas colunas de auditoria. "System" quando não autenticado.</summary>
    string AuditName { get; }
}
