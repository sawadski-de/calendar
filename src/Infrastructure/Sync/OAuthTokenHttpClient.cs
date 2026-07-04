using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Application.Sync;

namespace Infrastructure.Sync;

/// <summary>
/// Shared by <see cref="GoogleOAuthClient"/> and <see cref="MicrosoftOAuthClient"/> — both had an
/// identical POST-form/parse-response/compute-expiry sequence duplicated verbatim (code review
/// finding); both providers' token endpoints return the same shape (<c>access_token</c>,
/// <c>refresh_token</c>, <c>expires_in</c>), so this is the one place that parsing lives now.
/// </summary>
internal static class OAuthTokenHttpClient
{
    public static async Task<OAuthTokenResult> PostTokenRequestAsync(
        HttpClient httpClient,
        string tokenEndpoint,
        Dictionary<string, string> form,
        string failureErrorCode,
        string providerNameForErrorMessage,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new CalendarProviderException(failureErrorCode, $"{providerNameForErrorMessage} token endpoint returned {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenPayload>(cancellationToken: cancellationToken);
        if (payload?.AccessToken is null)
        {
            throw new CalendarProviderException(failureErrorCode, $"{providerNameForErrorMessage} token endpoint returned an empty/invalid body.");
        }

        var expiresAtUtc = timeProvider.GetUtcNow().AddSeconds(payload.ExpiresIn);
        return new OAuthTokenResult(payload.AccessToken, payload.RefreshToken, expiresAtUtc);
    }

    private class TokenPayload
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
