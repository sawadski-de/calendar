using System.Web;
using Microsoft.Extensions.Options;

namespace Infrastructure.Sync;

/// <summary>
/// Thin wrapper over Google's OAuth 2.0 token endpoint — plain <see cref="HttpClient"/>/REST rather
/// than the <c>Google.Apis.*</c> SDK, since this story needs exactly two calls (code exchange, token
/// refresh) and the Calendar `events.list` REST call (see <see cref="GoogleCalendarProvider"/>); pulling
/// in the full SDK for that would be more surface than value, and its .NET 10 compatibility hasn't been
/// verified.
/// </summary>
public class GoogleOAuthClient(HttpClient httpClient, IOptions<GoogleOAuthOptions> options, TimeProvider timeProvider)
{
    private const string Scope = "https://www.googleapis.com/auth/calendar.readonly";
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

    /// <summary>
    /// <c>access_type=offline</c>+<c>prompt=consent</c> together guarantee Google returns a refresh
    /// token on every connect attempt, not just the very first one a person ever makes for this app —
    /// without <c>prompt=consent</c>, a reconnect after a revoked/expired refresh token would silently
    /// come back with no refresh token at all.
    /// </summary>
    public string BuildAuthorizationUrl(string state)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = options.Value.ClientId;
        query["redirect_uri"] = options.Value.RedirectUri;
        query["response_type"] = "code";
        query["scope"] = Scope;
        query["access_type"] = "offline";
        query["prompt"] = "consent";
        query["state"] = state;
        return $"{AuthorizationEndpoint}?{query}";
    }

    public Task<OAuthTokenResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) =>
        OAuthTokenHttpClient.PostTokenRequestAsync(
            httpClient,
            TokenEndpoint,
            new Dictionary<string, string>
            {
                ["client_id"] = options.Value.ClientId,
                ["client_secret"] = options.Value.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = options.Value.RedirectUri,
            },
            "code_exchange_failed",
            "Google",
            timeProvider,
            cancellationToken);

    public Task<OAuthTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        OAuthTokenHttpClient.PostTokenRequestAsync(
            httpClient,
            TokenEndpoint,
            new Dictionary<string, string>
            {
                ["client_id"] = options.Value.ClientId,
                ["client_secret"] = options.Value.ClientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token",
            },
            "token_refresh_failed",
            "Google",
            timeProvider,
            cancellationToken);
}
