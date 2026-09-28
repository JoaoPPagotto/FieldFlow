using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Catalog;

/// <summary>Tipo de serviço do catálogo (ex.: manutenção preventiva), com SLA por prioridade.</summary>
public sealed class ServiceType : AggregateRoot
{
    public const int NameMaxLength = 100;

    private ServiceType() { } // EF Core

    public Guid ServiceTypeId { get; private set; }
    public string Name { get; private set; } = null!;
    public int DefaultDurationMinutes { get; private set; }
    public SlaPolicy Sla { get; private set; } = null!;

    public static Result<ServiceType> Create(string? name, int defaultDurationMinutes, SlaPolicy sla)
    {
        var validation = Validate(name, defaultDurationMinutes);
        if (validation.IsFailure) return validation.Error;

        return new ServiceType
        {
            ServiceTypeId = Guid.NewGuid(),
            Name = name!.Trim(),
            DefaultDurationMinutes = defaultDurationMinutes,
            Sla = sla
        };
    }

    public Result Update(string? name, int defaultDurationMinutes, SlaPolicy sla)
    {
        var validation = Validate(name, defaultDurationMinutes);
        if (validation.IsFailure) return validation;

        Name = name!.Trim();
        DefaultDurationMinutes = defaultDurationMinutes;
        Sla = sla;
        return Result.Success();
    }

    private static Result Validate(string? name, int defaultDurationMinutes)
        => Result.FirstFailureOrSuccess(
            Guard.Required(name, NameMaxLength, CatalogErrors.ServiceTypeNameRequired, CatalogErrors.ServiceTypeNameTooLong),
            defaultDurationMinutes is < 15 or > 1440
                ? Result.Failure(CatalogErrors.InvalidDefaultDuration)
                : Result.Success());
}
