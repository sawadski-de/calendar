namespace Infrastructure.Sync;

/// <summary>
/// Bound from <c>MICROSOFT_OAUTH_CLIENT_ID</c>/<c>MICROSOFT_OAUTH_CLIENT_SECRET</c>/
/// <c>MICROSOFT_OAUTH_REDIRECT_URI</c>/<c>MICROSOFT_OAUTH_TENANT_ID</c>. Both Api (initial handshake)
/// and Worker (token refresh) need all four — Microsoft's <c>refresh_token</c> grant requires
/// <c>client_id</c>+<c>client_secret</c> for a confidential client, same as Google (Story 2.1).
/// </summary>
public class MicrosoftOAuthOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// `[ASSUMPTION]` — "common" accepts consent from any Microsoft Entra tenant (multi-tenant app).
    /// If the team is on a single known Microsoft 365 tenant, a fixed tenant id would be the more
    /// restrictive/correct choice (see Story 2.2 Dev Notes "Offene Punkte" #2).
    /// </summary>
    public string TenantId { get; set; } = "common";
}
