using Application.Appointments;
using Domain;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Appointments;

public class AppointmentRepository(ApplicationDbContext dbContext) : IAppointmentRepository
{
    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, Appointment>> GetSyncedAppointmentsAsync(
        Guid personId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        var appointments = await dbContext.Appointments
            .Include(a => a.Attendees)
            .Where(a => a.PersonId == personId && a.Provider == provider && a.ProviderEventId != null)
            .ToListAsync(cancellationToken);

        // ProviderEventId is non-null for every row here (filtered above) — the partial unique index
        // (AD-7) already guarantees uniqueness per (PersonId, Provider, ProviderEventId).
        return appointments.ToDictionary(a => a.ProviderEventId!);
    }

    public async Task ApplySyncResultAsync(
        IReadOnlyList<Appointment> toInsert,
        IReadOnlyList<Guid> idsToDelete,
        CancellationToken cancellationToken = default)
    {
        if (idsToDelete.Count == 0 && toInsert.Count == 0)
        {
            return;
        }

        // Delete and insert must commit together — code review found the original two-step version
        // (ExecuteDeleteAsync then a separate SaveChangesAsync) could crash/fail between the steps and
        // permanently drop appointments (deleted, replacement never written). CreateExecutionStrategy
        // is required here because the DbContext has EnableRetryOnFailure() configured — an explicit
        // transaction needs the same retry-aware wrapping PersonRepository.ExecuteAtomicallyAsync uses.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            if (idsToDelete.Count > 0)
            {
                // ExecuteDeleteAsync avoids loading each appointment (and its attendees) into the
                // change tracker just to remove it — attendee rows cascade-delete at the DB level
                // (AD-7 delete path). It executes immediately against the DB but stays part of the
                // ambient transaction started above, so it rolls back together with the insert below.
                await dbContext.Appointments
                    .Where(a => idsToDelete.Contains(a.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            if (toInsert.Count > 0)
            {
                dbContext.Appointments.AddRange(toInsert);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        });
    }
}
