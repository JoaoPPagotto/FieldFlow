using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Assets;

public static class AssetErrors
{
    public static readonly Error TagRequired = Error.Validation("ASSET_TAG_REQUIRED", "Tag do ativo é obrigatória.");
    public static readonly Error TagTooLong = Error.Validation("ASSET_TAG_MAX_LENGTH_EXCEEDED", "Tag do ativo deve ter no máximo 50 caracteres.");
    public static readonly Error ModelRequired = Error.Validation("ASSET_MODEL_REQUIRED", "Modelo do ativo é obrigatório.");
    public static readonly Error ManufacturerRequired = Error.Validation("ASSET_MANUFACTURER_REQUIRED", "Fabricante do ativo é obrigatório.");
    public static readonly Error SerialNumberRequired = Error.Validation("ASSET_SERIAL_NUMBER_REQUIRED", "Número de série é obrigatório.");
    public static readonly Error CustomerRequired = Error.Validation("ASSET_CUSTOMER_REQUIRED", "Cliente do ativo é obrigatório.");
    public static readonly Error InvalidWarranty = Error.Validation("ASSET_WARRANTY_INVALID", "Garantia não pode terminar antes da instalação.");
    public static readonly Error TagAlreadyExists = Error.Conflict("ASSET_TAG_ALREADY_EXISTS", "Já existe um ativo com esta tag para o cliente.");
    public static readonly Error Decommissioned = Error.Conflict("ASSET_DECOMMISSIONED", "Ativo desativado não aceita alterações de status.");
    public static readonly Error NotFound = Error.NotFound("ASSET_NOT_FOUND", "Ativo não encontrado.");
}
