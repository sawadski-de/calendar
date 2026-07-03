using System.Security.Claims;
using Api.Contracts;
using Application.Sync;
using Domain;
using Infrastructure.Sync;
using Microsoft.AspNetCore.DataProtection;

namespace Api.Endpoints;

/// <summary>
/// OAuth handshake (initial connect only — AD-8, refresh belongs to the Worker) and per-user
/// connection-status reads for Settings → Kalenderverbindungen. Everything here is scoped to the
/// caller's own connections; there is no admin/bulk read (that's Story 2.3's Admin → Sync overview).
/// Google (Story 2.1) and Outlook (Story 2.2) share the same authorize/callback shape via
/// <see cref="HandleOAuthCallbackAsync"/> — only the OAuth client and error-code mapping differ.
/// </summary>
public static class CalendarConnectionEndpoints
{
    private const string StatePurpose = "CalendarConnectionOAuthState.v1";
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// `[ASSUMPTION]` — AD-16 says only "a threshold", not a number. Three consecutive failed attempts
    /// before the Settings row switches to the explicit error state (AC 11); a never-successful
    /// connection with any recorded error shows it immediately (AC 10), no threshold needed there.
    /// </summary>
    private const int RepeatedFailureThreshold = 3;

    public static void MapCalendarConnectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/calendar-connections").RequireAuthorization();

        group.MapGet("", async (
            ClaimsPrincipal user,
            ICalendarConnectionRepository calendarConnectionRepository,
            CancellationToken cancellationToken) =>
        {
            var personId = GetPersonId(user);
            var connections = await calendarConnectionRepository.GetByPersonAsync(personId, cancellationToken);
            var byProvider = connections.ToDictionary(c => c.Provider);

            var response = CalendarProviders.All.Select(provider =>
                byProvider.TryGetValue(provider, out var connection)
                    ? ToResponse(provider, connection)
                    : new CalendarConnectionResponse(provider, false, null, false, null));

            return Results.Ok(response);
        });

        group.MapGet("/google/authorize", (
            ClaimsPrincipal user,
            GoogleOAuthClient oAuthClient,
            IDataProtectionProvider dataProtectionProvider,
            TimeProvider timeProvider) =>
        {
            var state = BuildState(dataProtectionProvider, GetPersonId(user), timeProvider);
            return Results.Redirect(oAuthClient.BuildAuthorizationUrl(state));
        });

        group.MapGet("/google/callback", (
            string? code,
            string? state,
            string? error,
            ClaimsPrincipal user,
            GoogleOAuthClient oAuthClient,
            ITokenEncryption tokenEncryption,
            ICalendarConnectionRepository calendarConnectionRepository,
            IDataProtectionProvider dataProtectionProvider,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
            HandleOAuthCallbackAsync(
                CalendarProviders.Google,
                code,
                state,
                error,
                GetPersonId(user),
                oAuthClient.ExchangeCodeAsync,
                MapGoogleError,
                tokenEncryption,
                calendarConnectionRepository,
                dataProtectionProvider,
                timeProvider,
                cancellationToken));

        group.MapGet("/outlook/authorize", (
            ClaimsPrincipal user,
            MicrosoftOAuthClient oAuthClient,
            IDataProtectionProvider dataProtectionProvider,
            TimeProvider timeProvider) =>
        {
            var state = BuildState(dataProtectionProvider, GetPersonId(user), timeProvider);
            return Results.Redirect(oAuthClient.BuildAuthorizationUrl(state));
        });

        group.MapGet("/outlook/callback", (
            string? code,
            string? state,
            string? error,
            ClaimsPrincipal user,
            MicrosoftOAuthClient oAuthClient,
            ITokenEncryption tokenEncryption,
            ICalendarConnectionRepository calendarConnectionRepository,
            IDataProtectionProvider dataProtectionProvider,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
            HandleOAuthCallbackAsync(
                CalendarProviders.Outlook,
                code,
                state,
                error,
                GetPersonId(user),
                oAuthClient.ExchangeCodeAsync,
                MapMicrosoftError,
                tokenEncryption,
                calendarConnectionRepository,
                dataProtectionProvider,
                timeProvider,
                cancellationToken));
    }

    private static string BuildState(IDataProtectionProvider dataProtectionProvider, Guid personId, TimeProvider timeProvider)
    {
        var protector = dataProtectionProvider.CreateProtector(StatePurpose);
        return protector.Protect($"{personId:N}|{timeProvider.GetUtcNow():O}");
    }

    /// <summary>
    /// Shared body for every provider's OAuth callback — validates <c>state</c>, then either records a
    /// failure (consent denied/missing code/exchange error) or completes the connection. Both
    /// <c>Google</c> and <c>Outlook</c> (and any future provider) plug in via <paramref name="exchangeCodeAsync"/>
    /// and <paramref name="mapProviderError"/> only — the control flow itself must not be duplicated per
    /// provider (Story 2.1 built this for one provider; Story 2.2 generalized it for the second).
    /// </summary>
    private static async Task<IResult> HandleOAuthCallbackAsync(
        string provider,
        string? code,
        string? state,
        string? error,
        Guid personId,
        Func<string, CancellationToken, Task<GoogleTokenResult>> exchangeCodeAsync,
        Func<string, string> mapProviderError,
        ITokenEncryption tokenEncryption,
        ICalendarConnectionRepository calendarConnectionRepository,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var protector = dataProtectionProvider.CreateProtector(StatePurpose);

        // Binds this callback to the request that initiated it (same person, issued recently) —
        // without this, a crafted callback URL with someone else's authorization `code` could get
        // attached to whoever's browser happens to load it (an OAuth login-CSRF pattern).
        if (!TryValidateState(protector, state, personId, timeProvider))
        {
            return Results.Redirect("/settings/connections?error=invalid_state");
        }

        var connection = await calendarConnectionRepository.GetAsync(personId, provider, cancellationToken)
            ?? new CalendarConnection(Guid.NewGuid(), personId, provider);
        var now = timeProvider.GetUtcNow();

        if (error is not null)
        {
            var errorCode = mapProviderError(error);
            connection.RecordFailure(now, errorCode);
            await calendarConnectionRepository.UpsertAsync(connection, cancellationToken);
            return Results.Redirect($"/settings/connections?error={errorCode}");
        }

        if (code is null)
        {
            connection.RecordFailure(now, "code_missing");
            await calendarConnectionRepository.UpsertAsync(connection, cancellationToken);
            return Results.Redirect("/settings/connections?error=code_missing");
        }

        try
        {
            var tokenResult = await exchangeCodeAsync(code, cancellationToken);
            if (tokenResult.RefreshToken is null)
            {
                // Shouldn't happen given each client's offline-access request, but without a refresh
                // token the Worker could never sync past the first access token's short lifetime —
                // treat it as a failed connect rather than a half-working one.
                connection.RecordFailure(now, "no_refresh_token_returned");
                await calendarConnectionRepository.UpsertAsync(connection, cancellationToken);
                return Results.Redirect("/settings/connections?error=no_refresh_token_returned");
            }

            connection.MarkConnected(
                tokenEncryption.Encrypt(tokenResult.AccessToken),
                tokenEncryption.Encrypt(tokenResult.RefreshToken),
                tokenResult.ExpiresAtUtc,
                now);
            await calendarConnectionRepository.UpsertAsync(connection, cancellationToken);
            return Results.Redirect($"/settings/connections?connected={provider.ToLowerInvariant()}");
        }
        catch (CalendarProviderException ex)
        {
            connection.RecordFailure(now, ex.ErrorCode);
            await calendarConnectionRepository.UpsertAsync(connection, cancellationToken);
            return Results.Redirect($"/settings/connections?error={ex.ErrorCode}");
        }
    }

    private static Guid GetPersonId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static bool TryValidateState(IDataProtector protector, string? state, Guid personId, TimeProvider timeProvider)
    {
        if (string.IsNullOrEmpty(state))
        {
            return false;
        }

        string unprotected;
        try
        {
            unprotected = protector.Unprotect(state);
        }
        catch (Exception)
        {
            // Untrusted client input: a state value that isn't valid base64url at all throws
            // FormatException before decryption is even attempted; a tampered-but-well-formed payload
            // throws CryptographicException. Both mean the same thing here — reject as invalid state,
            // never let a malformed query param surface as an unhandled 500.
            return false;
        }

        var parts = unprotected.Split('|', 2);
        if (parts.Length != 2
            || !Guid.TryParse(parts[0], out var statePersonId)
            || !DateTimeOffset.TryParse(parts[1], out var issuedAtUtc))
        {
            return false;
        }

        return statePersonId == personId && timeProvider.GetUtcNow() - issuedAtUtc <= StateLifetime;
    }

    /// <summary>Google's OAuth `error` query values, mapped to this app's small stable error-code set (AD-13 spirit).</summary>
    private static string MapGoogleError(string googleError) => googleError switch
    {
        "access_denied" => "consent_denied",
        "admin_policy_enforced" => "tenant_blocked",
        _ => "oauth_error",
    };

    /// <summary>
    /// `[ASSUMPTION]` — Microsoft's tenant-blocked-self-consent error isn't as clean as Google's
    /// `admin_policy_enforced`; it typically surfaces as an `AADSTS...` code inside `error_description`
    /// rather than a distinct `error` value. Mapping only what's cleanly mappable here; verify against
    /// a real tenant-restricted account (Story 2.2 Dev Notes "Offene Punkte" #3).
    /// </summary>
    private static string MapMicrosoftError(string microsoftError) => microsoftError switch
    {
        "access_denied" => "consent_denied",
        "unauthorized_client" => "tenant_blocked",
        _ => "oauth_error",
    };

    private static CalendarConnectionResponse ToResponse(string provider, CalendarConnection connection) =>
        new(provider, connection.IsConnected, connection.LastSuccessfulSyncAt, HasVisibleError(connection), HasVisibleError(connection) ? connection.LastErrorCode : null);

    /// <summary>
    /// Shared by the per-user Settings row (here) and Story 2.3's Admin overview — both must agree on
    /// what "has an error" means for the exact same connection, or the two views could silently show
    /// contradictory states for the same account.
    /// </summary>
    internal static bool HasVisibleError(CalendarConnection connection) =>
        connection.LastErrorCode is not null && (!connection.IsConnected || connection.ConsecutiveFailureCount >= RepeatedFailureThreshold);
}
