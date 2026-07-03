using Application.Appointments;
using Domain;

namespace UnitTests.Application.Sync;

/// <summary>In-memory fake — mirrors <c>FakePersonRepository</c>'s convention (no EF Core/DB dependency).</summary>
public class FakeAppointmentRepository : IAppointmentRepository
{
    private readonly Dictionary<Guid, Appointment> _appointments = new();

    public IReadOnlyCollection<Appointment> All => _appointments.Values;

    /// <summary>When set, <see cref="ApplySyncResultAsync"/> throws this instead of applying the diff —
    /// simulates a DB error during persistence (code review: CalendarSyncService must catch and record
    /// this as a connection failure, not let it propagate uncaught).</summary>
    public Exception? ThrowOnApplySyncResult { get; set; }

    public Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _appointments[appointment.Id] = appointment;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, Appointment>> GetSyncedAppointmentsAsync(
        Guid personId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        var result = _appointments.Values
            .Where(a => a.PersonId == personId && a.Provider == provider && a.ProviderEventId is not null)
            .ToDictionary(a => a.ProviderEventId!);
        return Task.FromResult<IReadOnlyDictionary<string, Appointment>>(result);
    }

    public Task ApplySyncResultAsync(
        IReadOnlyList<Appointment> toInsert,
        IReadOnlyList<Guid> idsToDelete,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnApplySyncResult is not null)
        {
            throw ThrowOnApplySyncResult;
        }

        foreach (var id in idsToDelete)
        {
            _appointments.Remove(id);
        }

        foreach (var appointment in toInsert)
        {
            _appointments[appointment.Id] = appointment;
        }

        return Task.CompletedTask;
    }
}
