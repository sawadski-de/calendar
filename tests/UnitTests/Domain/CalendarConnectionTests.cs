using Domain;
using Xunit;

namespace UnitTests.Domain;

public class CalendarConnectionTests
{
    [Fact]
    public void UpdateTokensAfterRefresh_does_not_clear_an_ongoing_failure_streak()
    {
        // Code review regression: a token refresh happening mid-sync-cycle (before the calendar fetch
        // that follows even runs) must not look like a successful sync — only
        // CalendarSyncService.RecordSyncSuccess/RecordFailure, based on whether the FULL cycle
        // succeeded, may touch ConsecutiveFailureCount/LastErrorCode.
        var connection = new CalendarConnection(Guid.NewGuid(), Guid.NewGuid(), "Google");
        var now = DateTimeOffset.UtcNow;
        connection.MarkConnected("enc-access", "enc-refresh", now.AddHours(1), now.AddDays(-1));
        connection.RecordFailure(now, "provider_error");
        connection.RecordFailure(now, "provider_error");
        connection.RecordFailure(now, "provider_error");

        connection.UpdateTokensAfterRefresh("new-enc-access", "new-enc-refresh", now.AddHours(2));

        Assert.Equal(3, connection.ConsecutiveFailureCount);
        Assert.Equal("provider_error", connection.LastErrorCode);
        Assert.Equal("new-enc-access", connection.EncryptedAccessToken);
        Assert.Equal("new-enc-refresh", connection.EncryptedRefreshToken);
    }

    [Fact]
    public void MarkConnected_resets_the_failure_streak_because_it_is_itself_a_successful_attempt()
    {
        var connection = new CalendarConnection(Guid.NewGuid(), Guid.NewGuid(), "Google");
        var now = DateTimeOffset.UtcNow;
        connection.RecordFailure(now, "consent_denied");

        connection.MarkConnected("enc-access", "enc-refresh", now.AddHours(1), now);

        Assert.Equal(0, connection.ConsecutiveFailureCount);
        Assert.Null(connection.LastErrorCode);
        Assert.True(connection.IsConnected);
    }
}
