namespace FieldFlow.Application.Errors;

/// <summary>Mensagens de validação em pt-BR, com tokens do FluentValidation. Nunca use literal solto no validator.</summary>
public static class ValidationMessages
{
    public const string MaxLength      = "{PropertyName} deve ter no máximo {MaxLength} caracteres";
    public const string NotEmpty       = "{PropertyName} é obrigatório";
    public const string NotPositive    = "{PropertyName} deve ser um número positivo";
    public const string Invalid        = "{PropertyName} é inválido(a)";
    public const string Exists         = "{PropertyName} já está em uso";
    public const string NameExists     = "Nome já existe";
    public const string NotFound       = "{PropertyName} não encontrado(a)";
    public const string RelationExists = "Não é possível excluir {PropertyName}, pois está vinculado(a) a outros registros.";
}
