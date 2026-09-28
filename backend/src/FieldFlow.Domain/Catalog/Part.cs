using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Catalog;

/// <summary>Peça do catálogo, com controle simples de estoque.</summary>
public sealed class Part : AggregateRoot
{
    private Part() { } // EF Core

    public Guid PartId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    public static Result<Part> Create(string? code, string? name, decimal unitPrice, int stockQuantity)
    {
        var validation = Validate(code, name, unitPrice);
        if (validation.IsFailure) return validation.Error;
        if (stockQuantity < 0) return CatalogErrors.InvalidStock;

        return new Part
        {
            PartId = Guid.NewGuid(),
            Code = code!.Trim().ToUpperInvariant(),
            Name = name!.Trim(),
            UnitPrice = decimal.Round(unitPrice, 2),
            StockQuantity = stockQuantity
        };
    }

    public Result Update(string? code, string? name, decimal unitPrice)
    {
        var validation = Validate(code, name, unitPrice);
        if (validation.IsFailure) return validation;

        Code = code!.Trim().ToUpperInvariant();
        Name = name!.Trim();
        UnitPrice = decimal.Round(unitPrice, 2);
        return Result.Success();
    }

    /// <summary>Baixa de estoque ao usar a peça numa OS.</summary>
    public Result DecreaseStock(int quantity)
    {
        if (quantity <= 0) return CatalogErrors.InvalidQuantity;
        if (quantity > StockQuantity) return CatalogErrors.InsufficientStock(Code, StockQuantity, quantity);

        StockQuantity -= quantity;
        return Result.Success();
    }

    public Result IncreaseStock(int quantity)
    {
        if (quantity <= 0) return CatalogErrors.InvalidQuantity;
        StockQuantity += quantity;
        return Result.Success();
    }

    private static Result Validate(string? code, string? name, decimal unitPrice)
        => Result.FirstFailureOrSuccess(
            Guard.Required(code, 30, CatalogErrors.PartCodeRequired, CatalogErrors.PartCodeTooLong),
            Guard.NotEmpty(name, CatalogErrors.PartNameRequired),
            unitPrice < 0 ? Result.Failure(CatalogErrors.InvalidUnitPrice) : Result.Success());
}
