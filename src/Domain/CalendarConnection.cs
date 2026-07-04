namespace Domain;

/// <summary>
/// One connected (or attempted) external calendar account for a <see cref="Person"/> (Google or
/// Outlook, Story 2.1/2.2 share this entity — <see cref="Provider"/> is a plain string, not an enum, so
/// a new provider never requires a migration to widen an enum). Exactly one row per (PersonId,
/// Provider) — enforced by a unique index in <c>ApplicationDbContext</c>.
///
/// The row is created on the very first "Verbinden" click, before we know whether the OAuth handshake
/// will succeed — <see cref="EncryptedAccessToken"/>/<see cref="EncryptedRefreshToken"/> stay <c>null</c>
/// until the first successful exchange. This is what lets a consent-denied/tenant-blocked failure
/// (AC 10) show a persistent, explicit error on the Settings row even though the account was never
/// actually connected — without a row to hang the error on, that state would have nowhere to live.
/// </summary>
public class CalendarConnection
{
    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public string Provider { get; private set; }

    /// <summary>
    /// Ciphertext only (AD-8) — Domain never sees a plaintext token. Encryption/decryption happens
    /// exclusively in Infrastructure, immediately before a provider API call. Both null until the
    /// first successful <see cref="MarkConnected"/> call.
    /// </summary>
    public string? EncryptedAccessToken { get; private set; }
    public string? EncryptedRefreshToken { get; private set; }
    public DateTimeOffset? AccessTokenExpiresUtc { get; private set; }

    public bool IsConnected => EncryptedAccessToken is not null;

    public DateTimeOffset? LastSuccessfulSyncAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public int ConsecutiveFailureCount { get; private set; }
    public string? LastErrorCode { get; private set; }

    public CalendarConnection(Guid id, Guid personId, string provider)
    {
        Id = id;
        PersonId = personId;
        Provider = provider;
        ConsecutiveFailureCount = 0;
    }

    /// <summary>
    /// Called once, at the initial OAuth handshake (Api) only — this IS a successful attempt (the user
    /// just completed consent), so it's correct for this to reset the failure streak.
    /// </summary>
    public void MarkConnected(
        string encryptedAccessToken,
        string encryptedRefreshToken,
        DateTimeOffset accessTokenExpiresUtc,
        DateTimeOffset nowUtc)
    {
        EncryptedAccessToken = encryptedAccessToken;
        EncryptedRefreshToken = encryptedRefreshToken;
        AccessTokenExpiresUtc = accessTokenExpiresUtc;
        LastAttemptAt = nowUtc;
        ConsecutiveFailureCount = 0;
        LastErrorCode = null;
    }

    /// <summary>
    /// Called by the Worker mid-sync-cycle whenever the access token needed refreshing before the
    /// actual calendar fetch could run (AD-8). Deliberately does NOT touch
    /// <see cref="ConsecutiveFailureCount"/>/<see cref="LastErrorCode"/>/<see cref="LastAttemptAt"/> —
    /// a token refresh succeeding is not the same as the sync cycle succeeding (code review finding:
    /// calling <see cref="MarkConnected"/> here cleared an ongoing failure streak before the fetch that
    /// follows had even run, making a still-broken connection look healthy for up to
    /// <c>RepeatedFailureThreshold</c> cycles whenever a refresh happened to land mid-outage).
    /// <see cref="CalendarSyncService"/>'s <see cref="RecordSyncSuccess"/>/<see cref="RecordFailure"/>
    /// remain the only calls that update that bookkeeping, based on whether the full cycle succeeded.
    /// </summary>
    public void UpdateTokensAfterRefresh(string encryptedAccessToken, string encryptedRefreshToken, DateTimeOffset accessTokenExpiresUtc)
    {
        EncryptedAccessToken = encryptedAccessToken;
        EncryptedRefreshToken = encryptedRefreshToken;
        AccessTokenExpiresUtc = accessTokenExpiresUtc;
    }

    /// <summary>
    /// Resets the failure streak and clears any prior error — a repeated failure only becomes visible
    /// again after <see cref="ConsecutiveFailureCount"/> re-crosses the threshold (AD-16).
    /// </summary>
    public void RecordSyncSuccess(DateTimeOffset nowUtc)
    {
        LastSuccessfulSyncAt = nowUtc;
        LastAttemptAt = nowUtc;
        ConsecutiveFailureCount = 0;
        LastErrorCode = null;
    }

    /// <summary>
    /// Covers both a failed sync cycle (already connected) and a failed/denied initial handshake
    /// (never connected) — the Settings row renders both through the same error state (Dev Notes, AC
    /// 10 vs. AC 11). Leaves <see cref="LastSuccessfulSyncAt"/> untouched — that field must keep
    /// answering "when did this last actually work", not be overwritten by a failed attempt.
    /// </summary>
    public void RecordFailure(DateTimeOffset nowUtc, string errorCode)
    {
        LastAttemptAt = nowUtc;
        ConsecutiveFailureCount++;
        LastErrorCode = errorCode;
    }

    /// <summary>
    /// User-initiated "Verbindung trennen". Resets to the same clean slate as a never-attempted
    /// connection — a later reconnect goes through <see cref="MarkConnected"/> exactly like the first
    /// time, with no leftover error/failure state from before the disconnect.
    /// </summary>
    public void Disconnect()
    {
        EncryptedAccessToken = null;
        EncryptedRefreshToken = null;
        AccessTokenExpiresUtc = null;
        LastSuccessfulSyncAt = null;
        LastAttemptAt = null;
        ConsecutiveFailureCount = 0;
        LastErrorCode = null;
    }
}
