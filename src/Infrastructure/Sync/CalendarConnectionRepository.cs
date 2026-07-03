using Application.Sync;
using Domain;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Sync;

public class CalendarConnectionRepository(ApplicationDbContext dbContext) : ICalendarConnectionRepository
{
    public Task<CalendarConnection?> GetAsync(Guid personId, string provider, CancellationToken cancellationToken = default) =>
        dbContext.CalendarConnections
            .FirstOrDefaultAsync(c => c.PersonId == personId && c.Provider == provider, cancellationToken);

    public async Task<IReadOnlyList<CalendarConnection>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
        await dbContext.CalendarConnections.Where(c => c.PersonId == personId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CalendarConnection>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        // Person has no IsActive field yet (Epic 5 introduces it, AD-12) — every connection is "active"
        // for now. When that field lands, add `.Where(c => c.Person.IsActive)` here so a deactivated
        // person's connections stop being polled without touching any caller of this method.
        await dbContext.CalendarConnections.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CalendarConnection>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.CalendarConnections.ToListAsync(cancellationToken);

    public async Task UpsertAsync(CalendarConnection connection, CancellationToken cancellationToken = default)
    {
        var isTracked = dbContext.ChangeTracker.Entries<CalendarConnection>().Any(e => e.Entity.Id == connection.Id);
        if (!isTracked)
        {
            var exists = await dbContext.CalendarConnections.AnyAsync(c => c.Id == connection.Id, cancellationToken);
            if (exists)
            {
                dbContext.CalendarConnections.Update(connection);
            }
            else
            {
                dbContext.CalendarConnections.Add(connection);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
