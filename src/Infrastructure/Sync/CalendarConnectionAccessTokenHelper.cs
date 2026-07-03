using Application.Sync;
using Domain;

namespace Infrastructure.Sync;

/// <summary>
/// Shared by <see cref="GoogleCalendarProvider"/> and <see cref="OutlookCalendarProvider"/> — both had
/// an identical token-cache-check/refresh/re-encrypt/persist sequence duplicated verbatim (code review
/// finding); this is the one place that logic lives now.
/// </summary>
internal static class CalendarConnectionAccessTokenHelper
{
    public static async Task<string> GetValidAccessTokenAsync(
        CalendarConnection connection,
        Func<string, CancellationToken, Task<GoogleTokenResult>> refreshAsync,
        ITokenEncryption tokenEncryption,
        ICalendarConnectionRepository calendarConnectionRepository,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // One-minute safety margin so a token that's about to expire mid-request doesn't get used.
        if (connection.EncryptedAccessToken is not null
            && connection.AccessTokenExpiresUtc is { } expiresUtc
            && expiresUtc > now.AddMinutes(1))
        {
            return tokenEncryption.Decrypt(connection.EncryptedAccessToken);
        }

        if (connection.EncryptedRefreshToken is null)
        {
            throw new CalendarProviderException("token_refresh_failed", "No refresh token stored for this connection.");
        }

        var refreshToken = tokenEncryption.Decrypt(connection.EncryptedRefreshToken);
        var refreshed = await refreshAsync(refreshToken, cancellationToken);

        // The provider's refresh grant often omits a new refresh token entirely — keep the existing
        // one encrypted rather than overwriting it with null.
        var encryptedRefreshToken = refreshed.RefreshToken is not null
            ? tokenEncryption.Encrypt(refreshed.RefreshToken)
            : connection.EncryptedRefreshToken;
        var encryptedAccessToken = tokenEncryption.Encrypt(refreshed.AccessToken);

        // UpdateTokensAfterRefresh (not MarkConnected) — a token refresh succeeding here is not the
        // same as the sync cycle succeeding; CalendarSyncService's RecordSyncSuccess/RecordFailure are
        // the only calls allowed to touch the failure-streak bookkeeping (code review finding).
        connection.UpdateTokensAfterRefresh(encryptedAccessToken, encryptedRefreshToken, refreshed.ExpiresAtUtc);
        await calendarConnectionRepository.UpsertAsync(connection, cancellationToken);

        return refreshed.AccessToken;
    }
}
