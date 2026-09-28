using FieldFlow.Domain.Calendar;
using FieldFlow.Domain.Technicians;

namespace FieldFlow.Domain.UnitTests.Technicians;

public class TechnicianTests
{
    [Fact]
    public void Skills_can_be_added_once_and_removed()
    {
        var technician = Technician.Create("Carla", WorkingHours.Standard).Value;
        var serviceTypeId = Guid.NewGuid();

        technician.AddSkill(serviceTypeId).IsSuccess.Should().BeTrue();
        technician.AddSkill(serviceTypeId).Error.Code.Should().Be("TECHNICIAN_SKILL_ALREADY_ASSIGNED");
        technician.HasSkill(serviceTypeId).Should().BeTrue();

        technician.RemoveSkill(serviceTypeId).IsSuccess.Should().BeTrue();
        technician.HasSkill(serviceTypeId).Should().BeFalse();
        technician.RemoveSkill(serviceTypeId).Error.Code.Should().Be("TECHNICIAN_SKILL_NOT_ASSIGNED");
    }

    [Theory]
    [InlineData("2026-03-02 08:00", "2026-03-02 10:00", true)]   // segunda, início da jornada
    [InlineData("2026-03-02 16:00", "2026-03-02 18:00", true)]   // termina no limite
    [InlineData("2026-03-02 07:30", "2026-03-02 09:00", false)]  // começa antes
    [InlineData("2026-03-02 17:00", "2026-03-02 19:00", false)]  // termina depois
    [InlineData("2026-03-07 09:00", "2026-03-07 10:00", false)]  // sábado
    [InlineData("2026-03-02 17:00", "2026-03-03 09:00", false)]  // atravessa a noite
    public void IsWithinWorkingHours_checks_day_and_window(string start, string end, bool expected)
    {
        var technician = Technician.Create("Carla", WorkingHours.Standard).Value;

        var result = technician.IsWithinWorkingHours(Utc(start), Utc(end), TimeZoneInfo.Utc);

        result.Should().Be(expected);
    }

    [Fact]
    public void WorkingHours_validates_window_and_days()
    {
        WorkingHours.Create(new TimeOnly(18, 0), new TimeOnly(8, 0), WorkingDays.Weekdays)
            .Error.Code.Should().Be("TECHNICIAN_WORKING_HOURS_INVALID");
        WorkingHours.Create(new TimeOnly(8, 0), new TimeOnly(18, 0), WorkingDays.None)
            .Error.Code.Should().Be("TECHNICIAN_WORKING_DAYS_REQUIRED");
    }

    private static DateTime Utc(string value)
        => DateTime.SpecifyKind(DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc);
}
