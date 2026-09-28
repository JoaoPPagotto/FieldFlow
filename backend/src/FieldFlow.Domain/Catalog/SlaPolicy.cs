using FieldFlow.Domain.Common;
using FieldFlow.Domain.WorkOrders;

namespace FieldFlow.Domain.Catalog;

/// <summary>Horas de SLA (em horário comercial) por prioridade. Prioridade maior nunca tem prazo maior.</summary>
public sealed record SlaPolicy
{
    private SlaPolicy(int lowHours, int mediumHours, int highHours, int criticalHours)
    {
        LowHours = lowHours;
        MediumHours = mediumHours;
        HighHours = highHours;
        CriticalHours = criticalHours;
    }

    public int LowHours { get; }
    public int MediumHours { get; }
    public int HighHours { get; }
    public int CriticalHours { get; }

    public static Result<SlaPolicy> Create(int lowHours, int mediumHours, int highHours, int criticalHours)
    {
        if (lowHours <= 0 || mediumHours <= 0 || highHours <= 0 || criticalHours <= 0)
            return CatalogErrors.SlaHoursMustBePositive;
        if (!(criticalHours <= highHours && highHours <= mediumHours && mediumHours <= lowHours))
            return CatalogErrors.SlaHoursOutOfOrder;

        return new SlaPolicy(lowHours, mediumHours, highHours, criticalHours);
    }

    public int GetHours(Priority priority) => priority switch
    {
        Priority.Low => LowHours,
        Priority.Medium => MediumHours,
        Priority.High => HighHours,
        Priority.Critical => CriticalHours,
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
    };
}
