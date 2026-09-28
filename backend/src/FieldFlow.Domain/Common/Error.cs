namespace FieldFlow.Domain.Common;

/// <summary>Categoria do erro — a borda (Api) traduz para status HTTP.</summary>
public enum ErrorType
{
    /// <summary>Entrada inválida → 400.</summary>
    Validation,
    /// <summary>Recurso inexistente → 404.</summary>
    NotFound,
    /// <summary>Conflito de estado (transição inválida, agenda sobreposta, duplicidade) → 409.</summary>
    Conflict,
    /// <summary>Regra de negócio não atendida (pré-condição) → 422.</summary>
    BusinessRule,
    /// <summary>Sem permissão sobre o recurso → 403.</summary>
    Forbidden
}

/// <summary>Erro de domínio: código estável (SCREAMING_SNAKE_CASE) + mensagem legível.</summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Validation)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error BusinessRule(string code, string message) => new(code, message, ErrorType.BusinessRule);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
}
