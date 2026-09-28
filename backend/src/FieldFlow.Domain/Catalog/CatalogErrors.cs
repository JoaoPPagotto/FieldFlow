using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Catalog;

public static class CatalogErrors
{
    public static readonly Error ServiceTypeNameRequired = Error.Validation("SERVICE_TYPE_NAME_REQUIRED", "Nome do tipo de serviço é obrigatório.");
    public static readonly Error ServiceTypeNameTooLong = Error.Validation("SERVICE_TYPE_NAME_MAX_LENGTH_EXCEEDED", "Nome do tipo de serviço deve ter no máximo 100 caracteres.");
    public static readonly Error InvalidDefaultDuration = Error.Validation("SERVICE_TYPE_DURATION_INVALID", "Duração padrão deve estar entre 15 e 1440 minutos.");
    public static readonly Error SlaHoursMustBePositive = Error.Validation("SERVICE_TYPE_SLA_HOURS_INVALID", "Horas de SLA devem ser positivas.");
    public static readonly Error SlaHoursOutOfOrder = Error.Validation("SERVICE_TYPE_SLA_ORDER_INVALID", "SLA deve respeitar Crítica ≤ Alta ≤ Média ≤ Baixa.");
    public static readonly Error ServiceTypeNotFound = Error.NotFound("SERVICE_TYPE_NOT_FOUND", "Tipo de serviço não encontrado.");

    public static readonly Error PartCodeRequired = Error.Validation("PART_CODE_REQUIRED", "Código da peça é obrigatório.");
    public static readonly Error PartCodeTooLong = Error.Validation("PART_CODE_MAX_LENGTH_EXCEEDED", "Código da peça deve ter no máximo 30 caracteres.");
    public static readonly Error PartNameRequired = Error.Validation("PART_NAME_REQUIRED", "Nome da peça é obrigatório.");
    public static readonly Error InvalidUnitPrice = Error.Validation("PART_UNIT_PRICE_INVALID", "Preço unitário não pode ser negativo.");
    public static readonly Error InvalidQuantity = Error.Validation("PART_QUANTITY_INVALID", "Quantidade deve ser maior que zero.");
    public static readonly Error InvalidStock = Error.Validation("PART_STOCK_INVALID", "Estoque não pode ser negativo.");
    public static readonly Error PartCodeAlreadyExists = Error.Conflict("PART_CODE_ALREADY_EXISTS", "Já existe uma peça com este código.");
    public static readonly Error PartNotFound = Error.NotFound("PART_NOT_FOUND", "Peça não encontrada.");

    public static Error InsufficientStock(string code, int available, int requested)
        => Error.BusinessRule("PART_INSUFFICIENT_STOCK",
            $"Estoque insuficiente da peça {code}: disponível {available}, solicitado {requested}.");
}
