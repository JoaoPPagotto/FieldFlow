namespace FieldFlow.Infrastructure.Calendar;

/// <summary>Seção <c>BusinessCalendar</c> do appsettings.</summary>
public sealed class BusinessCalendarOptions
{
    public const string SectionName = "BusinessCalendar";

    public string TimeZoneId { get; set; } = "America/Sao_Paulo";
    public TimeOnly WorkdayStart { get; set; } = new(8, 0);
    public TimeOnly WorkdayEnd { get; set; } = new(18, 0);
    /// <summary>Array (não List) para que o valor do appsettings substitua o default em vez de somar.</summary>
    public DayOfWeek[] WorkingDays { get; set; } =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    /// <summary>Tempo de cache dos feriados carregados do banco.</summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(1);
}
