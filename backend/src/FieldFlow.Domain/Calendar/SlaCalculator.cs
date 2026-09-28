using FieldFlow.Domain.WorkOrders;

namespace FieldFlow.Domain.Calendar;

/// <summary>
/// Domain service puro (sem framework) que conta tempo apenas em <b>horário comercial</b>,
/// respeitando fim de semana e feriados.
/// <para>Ex.: OS aberta sexta 17:00 com SLA de 4h vence segunda 11:00 — não sábado 21:00.</para>
/// Todas as entradas/saídas são UTC; o cálculo é feito no fuso do <see cref="BusinessCalendar"/>.
/// </summary>
public sealed class SlaCalculator
{
    /// <summary>Acima desta fração do prazo consumida, a OS está "em risco".</summary>
    public const double AtRiskThreshold = 0.8;

    private const int MaxDaysScanned = 3660; // proteção contra calendários degenerados

    public SlaCalculator(BusinessCalendar calendar) => Calendar = calendar;

    public BusinessCalendar Calendar { get; }

    public DateTime CalculateDueAt(DateTime openedAtUtc, int slaHours)
        => AddBusinessTime(openedAtUtc, TimeSpan.FromHours(slaHours));

    /// <summary>Soma <paramref name="duration"/> de tempo comercial a partir de <paramref name="startUtc"/>.</summary>
    public DateTime AddBusinessTime(DateTime startUtc, TimeSpan duration)
    {
        EnsureUtc(startUtc, nameof(startUtc));
        if (duration <= TimeSpan.Zero) return startUtc;

        var cursor = ToLocal(startUtc);
        var remaining = duration;

        for (var guard = 0; guard < MaxDaysScanned; guard++)
        {
            var date = DateOnly.FromDateTime(cursor);
            if (!Calendar.IsBusinessDay(date))
            {
                cursor = NextDayStart(date);
                continue;
            }

            var dayStart = date.ToDateTime(Calendar.WorkdayStart);
            var dayEnd = date.ToDateTime(Calendar.WorkdayEnd);

            if (cursor < dayStart) cursor = dayStart;
            if (cursor >= dayEnd)
            {
                cursor = NextDayStart(date);
                continue;
            }

            var available = dayEnd - cursor;
            if (remaining <= available)
                return ToUtc(cursor + remaining);

            remaining -= available;
            cursor = NextDayStart(date);
        }

        throw new InvalidOperationException("Não foi possível calcular o prazo: calendário sem dias úteis suficientes.");
    }

    /// <summary>Tempo comercial decorrido entre dois instantes UTC.</summary>
    public TimeSpan BusinessTimeBetween(DateTime fromUtc, DateTime toUtc)
    {
        EnsureUtc(fromUtc, nameof(fromUtc));
        EnsureUtc(toUtc, nameof(toUtc));
        if (toUtc <= fromUtc) return TimeSpan.Zero;

        var from = ToLocal(fromUtc);
        var to = ToLocal(toUtc);
        var total = TimeSpan.Zero;

        for (var date = DateOnly.FromDateTime(from); date <= DateOnly.FromDateTime(to); date = date.AddDays(1))
        {
            if (!Calendar.IsBusinessDay(date)) continue;

            var start = Max(from, date.ToDateTime(Calendar.WorkdayStart));
            var end = Min(to, date.ToDateTime(Calendar.WorkdayEnd));
            if (end > start) total += end - start;
        }

        return total;
    }

    /// <summary>
    /// Situação de SLA derivada (nunca persistida): <c>Breached</c> se passou do prazo,
    /// <c>AtRisk</c> se consumiu mais de 80% do tempo comercial, senão <c>OnTime</c>.
    /// </summary>
    public SlaStatus Evaluate(DateTime openedAtUtc, DateTime dueAtUtc, DateTime referenceUtc)
    {
        if (referenceUtc > dueAtUtc) return SlaStatus.Breached;

        var total = BusinessTimeBetween(openedAtUtc, dueAtUtc);
        if (total <= TimeSpan.Zero) return SlaStatus.OnTime;

        var consumed = BusinessTimeBetween(openedAtUtc, referenceUtc);
        return consumed / total > AtRiskThreshold ? SlaStatus.AtRisk : SlaStatus.OnTime;
    }

    private DateTime NextDayStart(DateOnly date) => date.AddDays(1).ToDateTime(Calendar.WorkdayStart);

    private DateTime ToLocal(DateTime utc)
        => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc, Calendar.TimeZone), DateTimeKind.Unspecified);

    private DateTime ToUtc(DateTime local)
        => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Calendar.TimeZone);

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    private static void EnsureUtc(DateTime value, string name)
    {
        if (value.Kind == DateTimeKind.Local)
            throw new ArgumentException("Use DateTime em UTC.", name);
    }
}
