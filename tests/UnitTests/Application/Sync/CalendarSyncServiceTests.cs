using Application.Sync;
using Domain;
using UnitTests.Application;
using Xunit;

namespace UnitTests.Application.Sync;

public class CalendarSyncServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SyncAsync_inserts_a_new_event_with_the_computed_status()
    {
        var connection = new CalendarConnection(Guid.NewGuid(), Guid.NewGuid(), "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();
        var evt = new ExternalCalendarEvent("evt-1", "Standup", Now, Now.AddMinutes(15), false, null, []);
        var provider = FakeCalendarProvider.Returning(evt);
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, new FakePersonRepository(), new FixedTimeProvider(Now));

        var outcome = await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        Assert.Equal(SyncOutcome.Succeeded, outcome);
        var stored = Assert.Single(appointmentRepository.All);
        Assert.Equal("Standup", stored.Title);
        Assert.Equal("evt-1", stored.ProviderEventId);
        Assert.Equal(AvailabilityStatus.Unterbrechbar, stored.Status);
    }

    [Fact]
    public async Task SyncAsync_replaces_a_changed_event_without_creating_a_duplicate()
    {
        var personId = Guid.NewGuid();
        var connection = new CalendarConnection(Guid.NewGuid(), personId, "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();

        var original = new Appointment(Guid.NewGuid(), personId, "Standup", Now, Now.AddMinutes(15), "Google", "evt-1");
        original.AssignStatus(AvailabilityStatus.Unterbrechbar);
        await appointmentRepository.AddAsync(original);

        // Same ProviderEventId, moved and renamed at the source.
        var moved = new ExternalCalendarEvent("evt-1", "Standup (moved)", Now.AddHours(1), Now.AddHours(1).AddMinutes(15), false, null, []);
        var provider = FakeCalendarProvider.Returning(moved);
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, new FakePersonRepository(), new FixedTimeProvider(Now));

        await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        var stored = Assert.Single(appointmentRepository.All);
        Assert.Equal("Standup (moved)", stored.Title);
        Assert.Equal(Now.AddHours(1), stored.StartUtc);
        Assert.NotEqual(original.Id, stored.Id);
    }

    [Fact]
    public async Task SyncAsync_leaves_an_unchanged_event_untouched()
    {
        var personId = Guid.NewGuid();
        var connection = new CalendarConnection(Guid.NewGuid(), personId, "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();

        var original = new Appointment(Guid.NewGuid(), personId, "Standup", Now, Now.AddMinutes(15), "Google", "evt-1");
        original.AssignStatus(AvailabilityStatus.Unterbrechbar);
        await appointmentRepository.AddAsync(original);

        var unchanged = new ExternalCalendarEvent("evt-1", "Standup", Now, Now.AddMinutes(15), false, null, []);
        var provider = FakeCalendarProvider.Returning(unchanged);
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, new FakePersonRepository(), new FixedTimeProvider(Now));

        await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        var stored = Assert.Single(appointmentRepository.All);
        Assert.Equal(original.Id, stored.Id);
    }

    [Fact]
    public async Task SyncAsync_deletes_an_appointment_whose_key_is_missing_from_the_latest_snapshot()
    {
        var personId = Guid.NewGuid();
        var connection = new CalendarConnection(Guid.NewGuid(), personId, "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();

        var cancelled = new Appointment(Guid.NewGuid(), personId, "Cancelled meeting", Now, Now.AddMinutes(30), "Google", "evt-cancelled");
        cancelled.AssignStatus(AvailabilityStatus.Unterbrechbar);
        await appointmentRepository.AddAsync(cancelled);

        var provider = FakeCalendarProvider.Returning(); // empty snapshot — evt-cancelled is gone at the source
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, new FakePersonRepository(), new FixedTimeProvider(Now));

        await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        Assert.Empty(appointmentRepository.All);
    }

    [Fact]
    public async Task SyncAsync_records_a_failure_on_the_connection_when_the_provider_throws_and_does_not_touch_appointments()
    {
        var personId = Guid.NewGuid();
        var connection = new CalendarConnection(Guid.NewGuid(), personId, "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();

        var existing = new Appointment(Guid.NewGuid(), personId, "Standup", Now, Now.AddMinutes(15), "Google", "evt-1");
        existing.AssignStatus(AvailabilityStatus.Unterbrechbar);
        await appointmentRepository.AddAsync(existing);

        var provider = FakeCalendarProvider.FailingWith(new CalendarProviderException("token_refresh_failed", "boom"));
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, new FakePersonRepository(), new FixedTimeProvider(Now));

        var outcome = await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        Assert.Equal(SyncOutcome.Failed, outcome);
        Assert.Equal("token_refresh_failed", connection.LastErrorCode);
        Assert.Equal(1, connection.ConsecutiveFailureCount);
        Assert.Equal(1, connectionRepository.UpsertCallCount);
        // A failed fetch must never touch previously-synced appointments — the diff never runs.
        Assert.Single(appointmentRepository.All);
    }

    [Fact]
    public async Task SyncAsync_persists_the_connection_status_after_a_successful_cycle()
    {
        var connection = new CalendarConnection(Guid.NewGuid(), Guid.NewGuid(), "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();
        var provider = FakeCalendarProvider.Returning();
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, new FakePersonRepository(), new FixedTimeProvider(Now));

        await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        Assert.Equal(Now, connection.LastSuccessfulSyncAt);
        Assert.Equal(0, connection.ConsecutiveFailureCount);
        Assert.Equal(1, connectionRepository.UpsertCallCount);
    }

    [Fact]
    public async Task SyncAsync_records_a_generic_failure_when_the_provider_throws_a_non_CalendarProviderException()
    {
        // Code review regression: originally only CalendarProviderException was caught around the
        // fetch call — a token-decryption failure or any other unexpected exception propagated
        // uncaught, so the connection's error state was never updated.
        var connection = new CalendarConnection(Guid.NewGuid(), Guid.NewGuid(), "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();
        var provider = FakeCalendarProvider.FailingWith(new InvalidOperationException("token decrypt blew up"));
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, new FakePersonRepository(), new FixedTimeProvider(Now));

        var outcome = await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        Assert.Equal(SyncOutcome.Failed, outcome);
        Assert.Equal("unknown_error", connection.LastErrorCode);
        Assert.Equal(1, connection.ConsecutiveFailureCount);
    }

    [Fact]
    public async Task SyncAsync_records_a_failure_when_persisting_the_diff_throws()
    {
        // Code review regression: the original persistence step (ApplySyncResultAsync +
        // RecordSyncSuccess/UpsertAsync) ran outside any try/catch — a DB error there propagated
        // uncaught even though the provider fetch itself succeeded.
        var personId = Guid.NewGuid();
        var connection = new CalendarConnection(Guid.NewGuid(), personId, "Google");
        var appointmentRepository = new FakeAppointmentRepository { ThrowOnApplySyncResult = new TimeoutException("db unreachable") };
        var connectionRepository = new FakeCalendarConnectionRepository();
        var evt = new ExternalCalendarEvent("evt-1", "Standup", Now, Now.AddMinutes(15), false, null, []);
        var provider = FakeCalendarProvider.Returning(evt);
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, new FakePersonRepository(), new FixedTimeProvider(Now));

        var outcome = await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        Assert.Equal(SyncOutcome.Failed, outcome);
        Assert.Equal("unknown_error", connection.LastErrorCode);
        Assert.Equal(1, connection.ConsecutiveFailureCount);
    }

    [Fact]
    public async Task SyncAsync_does_not_record_a_failure_when_cancelled()
    {
        var connection = new CalendarConnection(Guid.NewGuid(), Guid.NewGuid(), "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();
        using var cts = new CancellationTokenSource();
        var provider = FakeCalendarProvider.FailingWith(new OperationCanceledException(cts.Token));
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, new FakePersonRepository(), new FixedTimeProvider(Now));
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)), cts.Token));

        Assert.Null(connection.LastErrorCode);
        Assert.Equal(0, connection.ConsecutiveFailureCount);
    }

    [Fact]
    public async Task SyncAsync_treats_an_attendee_email_case_change_as_unchanged()
    {
        // Code review regression: ordinal (case-sensitive) email comparison treated a provider-side
        // casing normalization as a real change, needlessly deleting and recreating the appointment.
        var personId = Guid.NewGuid();
        var connection = new CalendarConnection(Guid.NewGuid(), personId, "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();

        var original = new Appointment(Guid.NewGuid(), personId, "Standup", Now, Now.AddMinutes(15), "Google", "evt-1");
        original.AddExternalAttendee("Jonas@Example.com", "Jonas");
        original.AssignStatus(AvailabilityStatus.Unterbrechbar);
        await appointmentRepository.AddAsync(original);

        var sameAttendeeDifferentCase = new ExternalCalendarEvent(
            "evt-1", "Standup", Now, Now.AddMinutes(15), false, null, [new ExternalAttendee("jonas@example.com", "Jonas")]);
        var provider = FakeCalendarProvider.Returning(sameAttendeeDifferentCase);
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, new FakePersonRepository(), new FixedTimeProvider(Now));

        await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        var stored = Assert.Single(appointmentRepository.All);
        Assert.Equal(original.Id, stored.Id);
    }

    [Fact]
    public async Task SyncAsync_replaces_the_appointment_when_only_an_attendees_display_name_changed()
    {
        // Code review regression: HasChanged only diffed attendee emails — a provider-side display-name
        // update (email unchanged) was silently dropped and never persisted.
        var personId = Guid.NewGuid();
        var connection = new CalendarConnection(Guid.NewGuid(), personId, "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();

        var original = new Appointment(Guid.NewGuid(), personId, "Standup", Now, Now.AddMinutes(15), "Google", "evt-1");
        original.AddExternalAttendee("jonas@example.com", "Jonas Old Name");
        original.AssignStatus(AvailabilityStatus.Unterbrechbar);
        await appointmentRepository.AddAsync(original);

        var renamedAttendee = new ExternalCalendarEvent(
            "evt-1", "Standup", Now, Now.AddMinutes(15), false, null, [new ExternalAttendee("jonas@example.com", "Jonas New Name")]);
        var provider = FakeCalendarProvider.Returning(renamedAttendee);
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, new FakePersonRepository(), new FixedTimeProvider(Now));

        await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        var stored = Assert.Single(appointmentRepository.All);
        var attendee = Assert.Single(stored.Attendees);
        Assert.Equal("Jonas New Name", attendee.ExternalDisplayName);
    }

    [Fact]
    public async Task SyncAsync_attaches_an_attendee_as_a_real_teammate_when_the_email_matches_the_roster()
    {
        var personId = Guid.NewGuid();
        var connection = new CalendarConnection(Guid.NewGuid(), personId, "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();
        var teammate = new Person(Guid.NewGuid(), "jonas@example.com", PersonRole.Member);
        var personRepository = new FakePersonRepository(teammate);

        // Case differs from the roster entry — matching must be case-insensitive, same as HasChanged.
        var evt = new ExternalCalendarEvent(
            "evt-1", "Standup", Now, Now.AddMinutes(15), false, null, [new ExternalAttendee("Jonas@Example.com", "Jonas")]);
        var provider = FakeCalendarProvider.Returning(evt);
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, personRepository, new FixedTimeProvider(Now));

        await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        var stored = Assert.Single(appointmentRepository.All);
        var attendee = Assert.Single(stored.Attendees);
        Assert.Equal(teammate.Id, attendee.PersonId);
        Assert.Null(attendee.ExternalEmail);
    }

    [Fact]
    public async Task SyncAsync_treats_an_attendee_joining_the_roster_between_syncs_as_a_change()
    {
        // A team member is added after the appointment was first synced as external — the next sync
        // must upgrade the stored attendee to a real Person reference, not leave it external forever.
        var personId = Guid.NewGuid();
        var connection = new CalendarConnection(Guid.NewGuid(), personId, "Google");
        var appointmentRepository = new FakeAppointmentRepository();
        var connectionRepository = new FakeCalendarConnectionRepository();

        var original = new Appointment(Guid.NewGuid(), personId, "Standup", Now, Now.AddMinutes(15), "Google", "evt-1");
        original.AddExternalAttendee("jonas@example.com", "Jonas");
        original.AssignStatus(AvailabilityStatus.Unterbrechbar);
        await appointmentRepository.AddAsync(original);

        var teammate = new Person(Guid.NewGuid(), "jonas@example.com", PersonRole.Member);
        var personRepository = new FakePersonRepository(teammate);
        var evt = new ExternalCalendarEvent(
            "evt-1", "Standup", Now, Now.AddMinutes(15), false, null, [new ExternalAttendee("jonas@example.com", "Jonas")]);
        var provider = FakeCalendarProvider.Returning(evt);
        var sut = new CalendarSyncService(new FakeCalendarProviderResolver(provider), appointmentRepository, connectionRepository, personRepository, new FixedTimeProvider(Now));

        await sut.SyncAsync(connection, new SyncWindow(Now.AddDays(-1), Now.AddDays(1)));

        var stored = Assert.Single(appointmentRepository.All);
        var attendee = Assert.Single(stored.Attendees);
        Assert.Equal(teammate.Id, attendee.PersonId);
        Assert.NotEqual(original.Id, stored.Id);
    }
}
