using Domain;
using Xunit;

namespace UnitTests.Domain;

public class StatusHeuristicServiceTests
{
    private static Appointment CreateAppointment(TimeSpan duration, int attendeeCount, bool isAllDay = false)
    {
        var start = DateTimeOffset.UtcNow;
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), "Test", start, start + duration, isAllDay: isAllDay);
        for (var i = 0; i < attendeeCount; i++)
        {
            appointment.AddAttendee(Guid.NewGuid());
        }

        return appointment;
    }

    [Fact]
    public void No_attendees_is_always_Unterbrechbar_regardless_of_duration()
    {
        var appointment = CreateAppointment(TimeSpan.FromHours(5), attendeeCount: 0);

        Assert.Equal(AvailabilityStatus.Unterbrechbar, StatusHeuristicService.Compute(appointment));
    }

    [Fact]
    public void No_attendees_outranks_all_day_rule()
    {
        // AC 8: the no-attendee rule takes precedence over the all-day rule, regardless of duration.
        var appointment = CreateAppointment(TimeSpan.FromHours(24), attendeeCount: 0, isAllDay: true);

        Assert.Equal(AvailabilityStatus.Unterbrechbar, StatusHeuristicService.Compute(appointment));
    }

    [Fact]
    public void Exactly_45_minutes_with_one_attendee_is_Unterbrechbar()
    {
        // AC 6: lower bound of the "short" rule is inclusive.
        var appointment = CreateAppointment(TimeSpan.FromMinutes(45), attendeeCount: 1);

        Assert.Equal(AvailabilityStatus.Unterbrechbar, StatusHeuristicService.Compute(appointment));
    }

    [Fact]
    public void FortySix_minutes_with_one_attendee_is_still_Unterbrechbar()
    {
        // Just past the short-rule boundary, but not long/crowded enough for the long rule.
        var appointment = CreateAppointment(TimeSpan.FromMinutes(46), attendeeCount: 1);

        Assert.Equal(AvailabilityStatus.Unterbrechbar, StatusHeuristicService.Compute(appointment));
    }

    [Fact]
    public void Exactly_90_minutes_with_exactly_three_attendees_is_BitteNichtStoeren()
    {
        // AC 7: lower bound of the "long" rule is inclusive.
        var appointment = CreateAppointment(TimeSpan.FromMinutes(90), attendeeCount: 3);

        Assert.Equal(AvailabilityStatus.BitteNichtStoeren, StatusHeuristicService.Compute(appointment));
    }

    [Fact]
    public void Ninety_minutes_with_only_two_attendees_is_Unterbrechbar()
    {
        // Long duration alone isn't enough — needs the attendee-count threshold too.
        var appointment = CreateAppointment(TimeSpan.FromMinutes(90), attendeeCount: 2);

        Assert.Equal(AvailabilityStatus.Unterbrechbar, StatusHeuristicService.Compute(appointment));
    }

    [Fact]
    public void All_day_with_at_least_one_attendee_is_BitteNichtStoeren()
    {
        var appointment = CreateAppointment(TimeSpan.FromHours(24), attendeeCount: 1, isAllDay: true);

        Assert.Equal(AvailabilityStatus.BitteNichtStoeren, StatusHeuristicService.Compute(appointment));
    }
}
