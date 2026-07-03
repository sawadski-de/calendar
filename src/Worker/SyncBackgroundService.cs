using Application.Sync;
using Domain;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Worker;

/// <summary>
/// The sync loop (AD-2, AD-11, AD-16). Runs on a fixed interval, not real-time (NFR-1). Each connection
/// syncs in its own DI scope — <see cref="ApplicationDbContext"/> is not thread-safe, so a shared scope
/// across concurrent connections is not an option — and independently of the others: one connection's
/// failure must never stop the others from syncing (AD-16, AC 11).
/// </summary>
public class SyncBackgroundService(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<SyncBackgroundService> logger)
    : BackgroundService
{
    /// <summary>
    /// `[ASSUMPTION]` — NFR-1 only says "im Bereich weniger Minuten", no exact number. Configurable via
    /// `SYNC_INTERVAL_SECONDS` so this can be tuned without a code change; defaults to 5 minutes.
    /// </summary>
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(300);

    /// <summary>
    /// `[ASSUMPTION]` — no window size is specified anywhere in PRD/architecture for
    /// `ICalendarProvider.FetchAllEvents`. 30 days back / 180 days forward is a reasonable default for
    /// a team calendar tool, not a spec'd value.
    /// </summary>
    private static readonly TimeSpan WindowPast = TimeSpan.FromDays(30);
    private static readonly TimeSpan WindowFuture = TimeSpan.FromDays(180);

    /// <summary>
    /// Code review finding: syncing connections one at a time made a cycle's wall-clock time grow
    /// linearly with the number of connected accounts, risking a cycle overrunning
    /// `SYNC_INTERVAL_SECONDS` as the team grows. Bounded (not unlimited) so a large team doesn't open
    /// dozens of simultaneous DB connections/provider API calls at once.
    /// </summary>
    private const int MaxConcurrentSyncs = 8;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = GetConfiguredInterval();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOneCycleAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failure at the cycle level (e.g. DB unreachable) must not kill the whole Worker
                // process — the next timer tick tries again.
                logger.LogError(ex, "Sync cycle failed unexpectedly.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunOneCycleAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CalendarConnection> connections;
        using (var scope = scopeFactory.CreateScope())
        {
            var connectionRepository = scope.ServiceProvider.GetRequiredService<ICalendarConnectionRepository>();
            connections = await connectionRepository.GetAllActiveAsync(cancellationToken);
        }

        var now = timeProvider.GetUtcNow();
        var window = new SyncWindow(now - WindowPast, now + WindowFuture);
        var connectedOnly = connections.Where(c => c.IsConnected).ToList();

        await Parallel.ForEachAsync(
            connectedOnly,
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentSyncs, CancellationToken = cancellationToken },
            async (connection, ct) => await SyncOneConnectionAsync(connection, window, ct));
    }

    private async Task SyncOneConnectionAsync(CalendarConnection connection, SyncWindow window, CancellationToken cancellationToken)
    {
        try
        {
            // Own scope per connection — a scoped ApplicationDbContext must never be used concurrently
            // by two connections' syncs running in parallel.
            using var scope = scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<CalendarSyncService>();

            var outcome = await syncService.SyncAsync(connection, window, cancellationToken);
            if (outcome == SyncOutcome.Failed)
            {
                logger.LogWarning(
                    "Sync failed for person {PersonId} / {Provider}: {ErrorCode}",
                    connection.PersonId, connection.Provider, connection.LastErrorCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // CalendarSyncService already turns every failure it can anticipate into a recorded
            // CalendarConnection failure — this catch is the backstop for a genuine bug in that path,
            // and must still not block the rest of this cycle's connections (AD-16).
            logger.LogError(
                ex, "Unhandled error syncing person {PersonId} / {Provider}.", connection.PersonId, connection.Provider);
        }
    }

    private TimeSpan GetConfiguredInterval()
    {
        var raw = Environment.GetEnvironmentVariable("SYNC_INTERVAL_SECONDS");
        if (raw is not null && int.TryParse(raw, out var seconds) && seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return DefaultInterval;
    }
}
