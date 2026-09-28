using FieldFlow.Domain.Calendar;
using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Technicians;

/// <summary>Jornada do técnico (value object): início, fim e dias da semana.</summary>
public sealed record WorkingHours
{
    private WorkingHours(TimeOnly start, TimeOnly end, WorkingDays days)
    {
        Start = start;
        End = end;
        Days = days;
    }

    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public WorkingDays Days { get; }

    public static WorkingHours Standard { get; } = new(new TimeOnly(8, 0), new TimeOnly(18, 0), WorkingDays.Weekdays);

    public static Result<WorkingHours> Create(TimeOnly start, TimeOnly end, WorkingDays days)
    {
        if (end <= start) return TechnicianErrors.InvalidWorkingHours;
        if (days == WorkingDays.None) return TechnicianErrors.NoWorkingDays;
        return new WorkingHours(start, end, days);
    }

    /// <summary>A janela [start, end] (horário local) cabe inteira dentro de um único dia da jornada?</summary>
    public bool Covers(DateTime localStart, DateTime localEnd)
    {
        if (localEnd <= localStart) return false;
        if (localStart.Date != localEnd.Date) return false;
        if (!Days.Includes(localStart.DayOfWeek)) return false;

        return TimeOnly.FromDateTime(localStart) >= Start && TimeOnly.FromDateTime(localEnd) <= End;
    }
}
