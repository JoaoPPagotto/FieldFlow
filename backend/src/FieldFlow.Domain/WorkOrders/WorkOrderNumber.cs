using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.WorkOrders;

/// <summary>Número legível e sequencial: <c>OS-2026-000123</c>.</summary>
public sealed record WorkOrderNumber
{
    private WorkOrderNumber(string value) => Value = value;

    public string Value { get; }

    public static WorkOrderNumber From(int year, long sequence)
    {
        if (year is < 2000 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (sequence is <= 0 or > 999_999) throw new ArgumentOutOfRangeException(nameof(sequence));
        return new WorkOrderNumber($"OS-{year}-{sequence:D6}");
    }

    public static Result<WorkOrderNumber> Parse(string? value)
    {
        if (value is null || value.Length != 14 || !value.StartsWith("OS-", StringComparison.Ordinal) || value[7] != '-'
            || !int.TryParse(value.AsSpan(3, 4), out _) || !long.TryParse(value.AsSpan(8, 6), out _))
            return Error.Validation("WORK_ORDER_NUMBER_INVALID", "Número de OS inválido.");

        return new WorkOrderNumber(value);
    }

    public override string ToString() => Value;
}
