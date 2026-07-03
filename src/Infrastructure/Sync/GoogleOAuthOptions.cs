namespace Infrastructure.Sync;

/// <summary>
/// Bound from <c>GOOGLE_OAUTH_CLIENT_ID</c>/<c>GOOGLE_OAUTH_CLIENT_SECRET</c>/<c>GOOGLE_OAUTH_REDIRECT_URI</c>.
/// Both Api (initial handshake) and Worker (token refresh) need all three — Google's <c>refresh_token</c>
/// grant requires <c>client_id</c>+<c>client_secret</c> for a confidential ("Web application") OAuth
/// client, which is what the Internal-Workspace consent flow (PRD Offene Frage #1) uses.
/// </summary>
public class GoogleOAuthOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}
