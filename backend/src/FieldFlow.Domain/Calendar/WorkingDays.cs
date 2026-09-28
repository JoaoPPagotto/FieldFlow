namespace FieldFlow.Domain.Calendar;

/// <summary>Dias da semana como flags (persistido como inteiro).</summary>
[Flags]
public enum WorkingDays
{
    None = 0,
    Sunday = 1 << 0,
    Monday = 1 << 1,
    Tuesday = 1 << 2,
    Wednesday = 1 << 3,
    Thursday = 1 << 4,
    Friday = 1 << 5,
    Saturday = 1 << 6,

    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    All = Weekdays | Saturday | Sunday
}

public static class WorkingDaysExtensions
{
    public static WorkingDays ToFlag(this DayOfWeek day) => (WorkingDays)(1 << (int)day);

    public static bool Includes(this WorkingDays days, DayOfWeek day) => (days & day.ToFlag()) != 0;

    public static WorkingDays FromDays(IEnumerable<DayOfWeek> days)
        => days.Aggregate(WorkingDays.None, (acc, d) => acc | d.ToFlag());
}
