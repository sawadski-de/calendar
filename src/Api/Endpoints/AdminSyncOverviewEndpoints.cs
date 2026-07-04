using Api.Contracts;
using Application.Accounts;
using Application.Sync;

namespace Api.Endpoints;

/// <summary>
/// Admin → Sync-Übersicht (Story 2.3) — the only aggregated read across every person's calendar
/// connections, so it sits behind the same <c>"Admin"</c> policy as the rest of <c>/api/admin/*</c>
/// (AD-17).
/// </summary>
public static class AdminSyncOverviewEndpoints
{
    public static void MapAdminSyncOverviewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/admin").RequireAuthorization("Admin").MapGet("/calendar-connections", async (
            IPersonRepository personRepository,
            ICalendarConnectionRepository calendarConnectionRepository,
            CancellationToken cancellationToken) =>
        {
            // Deliberately sequential, not Task.WhenAll: IPersonRepository and ICalendarConnectionRepository
            // share one scoped ApplicationDbContext per request, and EF Core's DbContext throws
            // ("a second operation was started on this context instance before a previous operation
            // completed") if two queries run concurrently against it — confirmed by an intermittent
            // 500 when this was changed to Task.WhenAll during code review.
            var roster = await personRepository.GetAllAsync(cancellationToken);
            var byPersonAndProvider = (await calendarConnectionRepository.GetAllAsync(cancellationToken))
                .ToDictionary(c => (c.PersonId, c.Provider));

            var rows = roster.SelectMany(person => CalendarProviders.All.Select(provider =>
                byPersonAndProvider.TryGetValue((person.Id, provider), out var connection)
                    ? new AdminCalendarConnectionRow(
                        person.Id,
                        person.Email,
                        provider,
                        connection.IsConnected,
                        connection.LastSuccessfulSyncAt,
                        CalendarConnectionEndpoints.HasVisibleError(connection),
                        CalendarConnectionEndpoints.HasVisibleError(connection) ? connection.LastErrorCode : null)
                    : new AdminCalendarConnectionRow(person.Id, person.Email, provider, false, null, false, null)));

            return Results.Ok(rows);
        });
    }
}
