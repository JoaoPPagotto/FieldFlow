using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Customers;

public static class CustomerErrors
{
    public static readonly Error NameRequired = Error.Validation("CUSTOMER_NAME_REQUIRED", "Nome do cliente é obrigatório.");
    public static readonly Error NameTooLong = Error.Validation("CUSTOMER_NAME_MAX_LENGTH_EXCEEDED", "Nome do cliente deve ter no máximo 200 caracteres.");
    public static readonly Error InvalidDocument = Error.Validation("CUSTOMER_DOCUMENT_INVALID", "CNPJ inválido.");
    public static readonly Error InvalidEmail = Error.Validation("CUSTOMER_EMAIL_INVALID", "E-mail inválido.");
    public static readonly Error InvalidPhone = Error.Validation("CUSTOMER_PHONE_INVALID", "Telefone deve ter 10 ou 11 dígitos.");
    public static readonly Error DocumentAlreadyExists = Error.Conflict("CUSTOMER_DOCUMENT_ALREADY_EXISTS", "Já existe um cliente com este CNPJ.");
    public static readonly Error NotFound = Error.NotFound("CUSTOMER_NOT_FOUND", "Cliente não encontrado.");

    public static Error InvalidAddress(string field)
        => Error.Validation("CUSTOMER_ADDRESS_INVALID", $"Endereço inválido: campo '{field}'.");
}
