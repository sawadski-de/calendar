namespace Infrastructure.Sync;

/// <summary>Result of a token exchange/refresh call — shared by every provider's OAuth client (Google, Microsoft).</summary>
public record OAuthTokenResult(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAtUtc);
