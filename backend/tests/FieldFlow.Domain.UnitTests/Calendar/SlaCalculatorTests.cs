using FieldFlow.Domain.Calendar;
using FieldFlow.Domain.UnitTests.TestData;
using FieldFlow.Domain.WorkOrders;

namespace FieldFlow.Domain.UnitTests.Calendar;

public class SlaCalculatorTests
{
    private static readonly DateOnly Tiradentes = new(2026, 4, 21);   // terça
    private static readonly DateOnly GoodFriday = new(2026, 4, 3);    // sexta

    private static DateTime Utc(string value)
        => DateTime.SpecifyKind(DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc);

    [Theory]
    [InlineData("2026-03-02 09:00", 4, "2026-03-02 13:00")]  // mesmo dia
    [InlineData("2026-03-02 16:00", 4, "2026-03-03 10:00")]  // vira o dia
    [InlineData("2026-03-06 17:00", 4, "2026-03-09 11:00")]  // sexta 17h + 4h = segunda 11h
    [InlineData("2026-03-07 10:00", 2, "2026-03-09 10:00")]  // aberta no sábado
    [InlineData("2026-03-08 23:00", 1, "2026-03-09 09:00")]  // aberta no domingo à noite
    [InlineData("2026-03-02 06:00", 2, "2026-03-02 10:00")]  // antes do expediente
    [InlineData("2026-03-02 19:00", 1, "2026-03-03 09:00")]  // depois do expediente
    [InlineData("2026-03-02 08:00", 10, "2026-03-02 18:00")] // exatamente um dia comercial
    [InlineData("2026-03-02 09:00", 24, "2026-03-04 13:00")] // vários dias
    [InlineData("2026-03-06 18:00", 58, "2026-03-16 16:00")] // SLA longo cruzando dois fins de semana
    public void CalculateDueAt_counts_only_business_hours(string openedAt, int slaHours, string expectedDueAt)
    {
        var sut = DomainFixtures.Sla();

        var dueAt = sut.CalculateDueAt(Utc(openedAt), slaHours);

        dueAt.Should().Be(Utc(expectedDueAt));
    }

    [Theory]
    [InlineData("2026-04-20 16:00", 4, "2026-04-22 10:00")]  // pula Tiradentes (terça)
    [InlineData("2026-04-02 17:00", 4, "2026-04-06 11:00")]  // pula Sexta-feira Santa + fim de semana
    [InlineData("2026-04-21 10:00", 2, "2026-04-22 10:00")]  // aberta no próprio feriado
    public void CalculateDueAt_skips_holidays(string openedAt, int slaHours, string expectedDueAt)
    {
        var sut = DomainFixtures.Sla(Tiradentes, GoodFriday);

        sut.CalculateDueAt(Utc(openedAt), slaHours).Should().Be(Utc(expectedDueAt));
    }

    [Fact]
    public void CalculateDueAt_respects_calendar_time_zone()
    {
        var saoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); // UTC-3, sem horário de verão
        var sut = new SlaCalculator(BusinessCalendar.Default(saoPaulo));

        // sexta 17:00 em São Paulo = 20:00 UTC → segunda 11:00 em São Paulo = 14:00 UTC
        var dueAt = sut.CalculateDueAt(Utc("2026-03-06 20:00"), 4);

        dueAt.Should().Be(Utc("2026-03-09 14:00"));
        dueAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void AddBusinessTime_with_zero_duration_returns_start()
    {
        var start = DomainFixtures.MondayAt(20);

        DomainFixtures.Sla().AddBusinessTime(start, TimeSpan.Zero).Should().Be(start);
    }

    [Theory]
    [InlineData("2026-03-02 09:00", "2026-03-02 12:30", 3.5)]
    [InlineData("2026-03-06 17:00", "2026-03-09 11:00", 4)]    // fim de semana não conta
    [InlineData("2026-03-02 07:00", "2026-03-02 20:00", 10)]   // fora do expediente não conta
    [InlineData("2026-03-02 12:00", "2026-03-02 09:00", 0)]    // intervalo invertido
    [InlineData("2026-04-20 17:00", "2026-04-22 09:00", 2)]    // feriado no meio
    public void BusinessTimeBetween_counts_only_business_hours(string from, string to, double expectedHours)
    {
        var sut = DomainFixtures.Sla(Tiradentes);

        sut.BusinessTimeBetween(Utc(from), Utc(to)).Should().Be(TimeSpan.FromHours(expectedHours));
    }

    [Theory]
    [InlineData("2026-03-02 15:00", SlaStatus.OnTime)]    // 70% consumido
    [InlineData("2026-03-02 16:00", SlaStatus.OnTime)]    // exatamente 80%: ainda no prazo
    [InlineData("2026-03-02 17:00", SlaStatus.AtRisk)]    // 90% consumido
    [InlineData("2026-03-02 18:00", SlaStatus.AtRisk)]    // no limite do prazo
    [InlineData("2026-03-02 18:01", SlaStatus.Breached)]
    [InlineData("2026-03-05 09:00", SlaStatus.Breached)]
    public void Evaluate_derives_sla_status(string reference, SlaStatus expected)
    {
        var sut = DomainFixtures.Sla();
        var openedAt = DomainFixtures.MondayAt(8);
        var dueAt = DomainFixtures.MondayAt(18);

        sut.Evaluate(openedAt, dueAt, Utc(reference)).Should().Be(expected);
    }

    [Fact]
    public void Calendar_rejects_workday_that_ends_before_it_starts()
    {
        var act = () => new BusinessCalendar(TimeZoneInfo.Utc, new TimeOnly(18, 0), new TimeOnly(8, 0), WorkingDays.Weekdays);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Calendar_rejects_no_working_days()
    {
        var act = () => new BusinessCalendar(TimeZoneInfo.Utc, new TimeOnly(8, 0), new TimeOnly(18, 0), WorkingDays.None);

        act.Should().Throw<ArgumentException>();
    }
}
