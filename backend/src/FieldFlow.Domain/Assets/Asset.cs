using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Assets;

/// <summary>Equipamento instalado no cliente. A <see cref="Tag"/> é única por cliente.</summary>
public sealed class Asset : AggregateRoot
{
    private Asset() { } // EF Core

    public Guid AssetId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Tag { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public string Manufacturer { get; private set; } = null!;
    public string SerialNumber { get; private set; } = null!;
    public DateOnly? InstalledAt { get; private set; }
    public DateOnly? WarrantyUntil { get; private set; }
    public AssetStatus Status { get; private set; }

    public bool IsUnderWarranty(DateOnly date) => WarrantyUntil is { } until && date <= until;

    public static Result<Asset> Create(Guid customerId, string? tag, string? model, string? manufacturer,
                                       string? serialNumber, DateOnly? installedAt, DateOnly? warrantyUntil)
    {
        if (customerId == Guid.Empty) return AssetErrors.CustomerRequired;

        var validation = Validate(tag, model, manufacturer, serialNumber, installedAt, warrantyUntil);
        if (validation.IsFailure) return validation.Error;

        return new Asset
        {
            AssetId = Guid.NewGuid(),
            CustomerId = customerId,
            Tag = tag!.Trim().ToUpperInvariant(),
            Model = model!.Trim(),
            Manufacturer = manufacturer!.Trim(),
            SerialNumber = serialNumber!.Trim(),
            InstalledAt = installedAt,
            WarrantyUntil = warrantyUntil,
            Status = AssetStatus.Active
        };
    }

    public Result Update(string? tag, string? model, string? manufacturer, string? serialNumber,
                         DateOnly? installedAt, DateOnly? warrantyUntil)
    {
        var validation = Validate(tag, model, manufacturer, serialNumber, installedAt, warrantyUntil);
        if (validation.IsFailure) return validation;

        Tag = tag!.Trim().ToUpperInvariant();
        Model = model!.Trim();
        Manufacturer = manufacturer!.Trim();
        SerialNumber = serialNumber!.Trim();
        InstalledAt = installedAt;
        WarrantyUntil = warrantyUntil;
        return Result.Success();
    }

    public Result MarkUnderMaintenance() => ChangeStatus(AssetStatus.UnderMaintenance);

    public Result Reactivate() => ChangeStatus(AssetStatus.Active);

    public Result Decommission() => ChangeStatus(AssetStatus.Decommissioned);

    private Result ChangeStatus(AssetStatus status)
    {
        if (Status == AssetStatus.Decommissioned) return AssetErrors.Decommissioned;
        Status = status;
        return Result.Success();
    }

    private static Result Validate(string? tag, string? model, string? manufacturer, string? serialNumber,
                                   DateOnly? installedAt, DateOnly? warrantyUntil)
        => Result.FirstFailureOrSuccess(
            Guard.Required(tag, 50, AssetErrors.TagRequired, AssetErrors.TagTooLong),
            Guard.NotEmpty(model, AssetErrors.ModelRequired),
            Guard.NotEmpty(manufacturer, AssetErrors.ManufacturerRequired),
            Guard.NotEmpty(serialNumber, AssetErrors.SerialNumberRequired),
            installedAt is { } i && warrantyUntil is { } w && w < i
                ? Result.Failure(AssetErrors.InvalidWarranty)
                : Result.Success());
}
