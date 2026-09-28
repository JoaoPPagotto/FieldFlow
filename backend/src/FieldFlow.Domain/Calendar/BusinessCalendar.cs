namespace FieldFlow.Domain.Calendar;

/// <summary>
/// Calendário comercial usado no cálculo de SLA: fuso, janela diária, dias úteis e feriados.
/// Imutável. Default: seg–sex, 08:00–18:00.
/// </summary>
public sealed class BusinessCalendar
{
    private readonly HashSet<DateOnly> _holidays;

    public BusinessCalendar(
        TimeZoneInfo timeZone,
        TimeOnly workdayStart,
        TimeOnly workdayEnd,
        WorkingDays workingDays,
        IEnumerable<DateOnly>? holidays = null)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        if (workdayEnd <= workdayStart)
            throw new ArgumentException("O fim do expediente deve ser posterior ao início.", nameof(workdayEnd));
        if (workingDays == WorkingDays.None)
            throw new ArgumentException("O calendário precisa de pelo menos um dia útil.", nameof(workingDays));

        TimeZone = timeZone;
        WorkdayStart = workdayStart;
        WorkdayEnd = workdayEnd;
        WorkingDays = workingDays;
        _holidays = holidays is null ? [] : [.. holidays];
    }

    public static BusinessCalendar Default(TimeZoneInfo timeZone, IEnumerable<DateOnly>? holidays = null)
        => new(timeZone, new TimeOnly(8, 0), new TimeOnly(18, 0), WorkingDays.Weekdays, holidays);

    public TimeZoneInfo TimeZone { get; }
    public TimeOnly WorkdayStart { get; }
    public TimeOnly WorkdayEnd { get; }
    public WorkingDays WorkingDays { get; }
    public IReadOnlyCollection<DateOnly> Holidays => _holidays;

    public bool IsBusinessDay(DateOnly date)
        => WorkingDays.Includes(date.DayOfWeek) && !_holidays.Contains(date);
}
