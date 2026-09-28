namespace FieldFlow.Application.Errors;

/// <summary>Códigos de erro de infraestrutura/HTTP. Erros de negócio vivem no domínio (<c>*Errors</c>).</summary>
public static class ErrorCodes
{
    // Genéricos
    public const string NameRequired             = "NAME_REQUIRED";
    public const string NameExists               = "NAME_ALREADY_EXISTS";
    public const string NameMaxLengthExceeded    = "NAME_MAX_LENGTH_EXCEEDED";
    public const string DeleteIdMissingOrInvalid = "DELETE_ID_MISSING_OR_INVALID";

    // Infra / HTTP
    public const string ResourceNotFound      = "RESOURCE_NOT_FOUND";
    public const string ValidationFailed      = "VALIDATION_FAILED";
    public const string Unauthorized          = "UNAUTHORIZED";
    public const string Forbidden             = "FORBIDDEN";
    public const string Conflict              = "CONFLICT";
    public const string InvalidArgument       = "INVALID_ARGUMENT";
    public const string InvalidOperation      = "INVALID_OPERATION";
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    public const string DatabaseError         = "DATABASE_ERROR";
    public const string InternalError         = "INTERNAL_ERROR";
}
