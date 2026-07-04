namespace Api.Contracts;

/// <summary>
/// <c>HasError</c>/<c>ErrorCode</c> cover both "never connected, consent failed" (AC 10, shown
/// immediately) and "was connected, sync now fails repeatedly" (AC 11, shown once the failure streak
/// crosses the threshold) — the Settings row renders both through this one shape.
/// </summary>
public record CalendarConnectionResponse(
    string Provider,
    bool Connected,
    DateTimeOffset? LastSuccessfulSyncAt,
    bool HasError,
    string? ErrorCode);

/// <summary>One row of the Admin → Sync-Übersicht table (Story 2.3) — one per (person, provider), including people with no connection at all.</summary>
public record AdminCalendarConnectionRow(
    Guid PersonId,
    string PersonEmail,
    string Provider,
    bool Connected,
    DateTimeOffset? LastSuccessfulSyncAt,
    bool HasError,
    string? ErrorCode);
