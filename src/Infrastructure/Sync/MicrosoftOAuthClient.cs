using System.Web;
using Microsoft.Extensions.Options;

namespace Infrastructure.Sync;

/// <summary>
/// Thin wrapper over the Microsoft identity platform's OAuth 2.0 token endpoint — plain
/// <see cref="HttpClient"/>/REST, mirroring <see cref="GoogleOAuthClient"/> exactly (Story 2.1's
/// decision to avoid a provider SDK applies identically here).
/// </summary>
public class MicrosoftOAuthClient(HttpClient httpClient, IOptions<MicrosoftOAuthOptions> options, TimeProvider timeProvider)
{
    private const string Scope = "Calendars.Read offline_access";

    private string AuthorizationEndpoint => $"https://login.microsoftonline.com/{options.Value.TenantId}/oauth2/v2.0/authorize";
    private string TokenEndpoint => $"https://login.microsoftonline.com/{options.Value.TenantId}/oauth2/v2.0/token";

    /// <summary>
    /// Unlike Google, there is no separate <c>access_type=offline</c>/<c>prompt=consent</c> pair —
    /// including <c>offline_access</c> in the requested scope is what gets a refresh token back.
    /// Omitting it silently means every future sync needs a fresh interactive consent.
    /// </summary>
    public string BuildAuthorizationUrl(string state)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = options.Value.ClientId;
        query["redirect_uri"] = options.Value.RedirectUri;
        query["response_type"] = "code";
        query["scope"] = Scope;
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
                ["scope"] = Scope,
            },
            "code_exchange_failed",
            "Microsoft",
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
                ["scope"] = Scope,
            },
            "token_refresh_failed",
            "Microsoft",
            timeProvider,
            cancellationToken);
}
