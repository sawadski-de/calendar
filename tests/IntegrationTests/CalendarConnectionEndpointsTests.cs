using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Sync;
using Domain;
using IntegrationTests.Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Covers what's safely testable without a real Google OAuth app registration (Story 2.1 Dev Notes,
/// "Offene Punkte" #1) — status reads, token-at-rest encryption, the authorize redirect (pure URL
/// building, no outbound call), and the consent-denied/invalid-state callback branches (no code
/// exchange involved). The actual code-exchange-with-Google path is out of scope for automated tests
/// until real credentials exist; it's a manual verification step for Dennis post-merge.
/// </summary>
[Collection("Postgres")]
public class CalendarConnectionEndpointsTests(PostgresContainerFixture postgres)
{
    private async Task<(TestApiFactory Factory, HttpClient Client, Guid PersonId)> CreateAuthenticatedContextAsync(
        bool allowAutoRedirect = true)
    {
        var factory = new TestApiFactory(postgres.Container.GetConnectionString());
        // Redirect-following must be off for any test that asserts a 302 status itself (the
        // authorize/callback endpoints) — otherwise the client silently follows to the next hop
        // (accounts.google.com for authorize; the frontend route for callback) and the assertion
        // never sees the redirect response at all.
        var client = allowAutoRedirect
            ? factory.CreateHttpsClient()
            : factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false,
            });

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestApiFactory.AdminEmail,
            password = TestApiFactory.AdminPassword,
        });
        login.EnsureSuccessStatusCode();

        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        var personId = Guid.Parse(me.GetProperty("id").GetString()!);

        return (factory, client, personId);
    }

    [Fact]
    public async Task Returns_not_connected_for_both_providers_for_a_fresh_user()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var response = await client.GetFromJsonAsync<JsonElement>("/api/calendar-connections");
        var entries = response.EnumerateArray().ToArray();

        Assert.Equal(2, entries.Length);
        Assert.All(entries, e => Assert.False(e.GetProperty("connected").GetBoolean()));
        Assert.All(entries, e => Assert.False(e.GetProperty("hasError").GetBoolean()));
    }

    [Fact]
    public async Task Reflects_a_connected_account_including_last_successful_sync()
    {
        var (factory, client, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var syncedAt = DateTimeOffset.UtcNow.AddMinutes(-3);
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tokenEncryption = scope.ServiceProvider.GetRequiredService<ITokenEncryption>();
            var connection = new CalendarConnection(Guid.NewGuid(), personId, CalendarProviders.Google);
            connection.MarkConnected(
                tokenEncryption.Encrypt("access-token-value"),
                tokenEncryption.Encrypt("refresh-token-value"),
                DateTimeOffset.UtcNow.AddHours(1),
                syncedAt);
            connection.RecordSyncSuccess(syncedAt);
            dbContext.CalendarConnections.Add(connection);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetFromJsonAsync<JsonElement>("/api/calendar-connections");
        var google = response.EnumerateArray().First(e => e.GetProperty("provider").GetString() == "Google");

        Assert.True(google.GetProperty("connected").GetBoolean());
        // Postgres timestamptz round-trips at microsecond precision; DateTimeOffset ticks are 100ns —
        // compare with a small tolerance instead of exact equality.
        var returned = google.GetProperty("lastSuccessfulSyncAt").GetDateTimeOffset();
        Assert.True((returned - syncedAt).Duration() < TimeSpan.FromMilliseconds(1), $"Expected ~{syncedAt}, got {returned}");
    }

    [Fact]
    public async Task Never_stores_the_plaintext_token_in_the_database()
    {
        var (factory, _, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;

        const string plaintextRefreshToken = "plaintext-refresh-token-should-not-appear-in-db";

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenEncryption = scope.ServiceProvider.GetRequiredService<ITokenEncryption>();
        var connection = new CalendarConnection(Guid.NewGuid(), personId, CalendarProviders.Google);
        connection.MarkConnected(
            tokenEncryption.Encrypt("access-token-value"),
            tokenEncryption.Encrypt(plaintextRefreshToken),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow);
        dbContext.CalendarConnections.Add(connection);
        await dbContext.SaveChangesAsync();

        var storedCiphertext = await dbContext.CalendarConnections
            .Where(c => c.Id == connection.Id)
            .Select(c => c.EncryptedRefreshToken)
            .SingleAsync();

        Assert.DoesNotContain(plaintextRefreshToken, storedCiphertext);
        Assert.Equal(plaintextRefreshToken, tokenEncryption.Decrypt(storedCiphertext!));
    }

    [Fact]
    public async Task Shows_the_repeated_failure_error_state_once_the_threshold_is_crossed()
    {
        var (factory, client, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tokenEncryption = scope.ServiceProvider.GetRequiredService<ITokenEncryption>();
            var now = DateTimeOffset.UtcNow;
            var connection = new CalendarConnection(Guid.NewGuid(), personId, CalendarProviders.Google);
            connection.MarkConnected(tokenEncryption.Encrypt("a"), tokenEncryption.Encrypt("r"), now.AddHours(1), now.AddHours(-1));
            connection.RecordFailure(now, "token_refresh_failed");
            connection.RecordFailure(now, "token_refresh_failed");
            connection.RecordFailure(now, "token_refresh_failed");
            dbContext.CalendarConnections.Add(connection);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetFromJsonAsync<JsonElement>("/api/calendar-connections");
        var google = response.EnumerateArray().First(e => e.GetProperty("provider").GetString() == "Google");

        Assert.True(google.GetProperty("hasError").GetBoolean());
        Assert.Equal("token_refresh_failed", google.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Does_not_show_an_error_for_a_single_transient_failure_below_the_threshold()
    {
        var (factory, client, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tokenEncryption = scope.ServiceProvider.GetRequiredService<ITokenEncryption>();
            var now = DateTimeOffset.UtcNow;
            var connection = new CalendarConnection(Guid.NewGuid(), personId, CalendarProviders.Google);
            connection.MarkConnected(tokenEncryption.Encrypt("a"), tokenEncryption.Encrypt("r"), now.AddHours(1), now.AddHours(-1));
            connection.RecordFailure(now, "provider_error");
            dbContext.CalendarConnections.Add(connection);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetFromJsonAsync<JsonElement>("/api/calendar-connections");
        var google = response.EnumerateArray().First(e => e.GetProperty("provider").GetString() == "Google");

        Assert.False(google.GetProperty("hasError").GetBoolean());
    }

    [Fact]
    public async Task Authorize_redirects_to_google_with_the_required_query_parameters_and_no_outbound_call()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync(allowAutoRedirect: false);
        using var f = factory;
        using var c = client;

        var response = await client.GetAsync("/api/calendar-connections/google/authorize");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("https://accounts.google.com/o/oauth2/v2/auth", location);
        Assert.Contains("access_type=offline", location);
        Assert.Contains("prompt=consent", location);
        Assert.Contains("scope=https", location);
    }

    [Fact]
    public async Task Authorize_redirects_to_microsoft_with_offline_access_scope_and_no_outbound_call()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync(allowAutoRedirect: false);
        using var f = factory;
        using var c = client;

        var response = await client.GetAsync("/api/calendar-connections/outlook/authorize");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("https://login.microsoftonline.com/common/oauth2/v2.0/authorize", location);
        Assert.Contains("offline_access", Uri.UnescapeDataString(location));
        Assert.Contains("Calendars.Read", Uri.UnescapeDataString(location));
    }

    [Fact]
    public async Task Outlook_callback_with_a_consent_denied_error_records_a_persistent_error_without_ever_connecting()
    {
        var (factory, client, personId) = await CreateAuthenticatedContextAsync(allowAutoRedirect: false);
        using var f = factory;
        using var c = client;

        var state = await BuildValidStateAsync(factory, personId);

        var response = await client.GetAsync($"/api/calendar-connections/outlook/callback?error=access_denied&state={Uri.EscapeDataString(state)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("error=consent_denied", response.Headers.Location!.ToString());

        var statusResponse = await client.GetFromJsonAsync<JsonElement>("/api/calendar-connections");
        var outlook = statusResponse.EnumerateArray().First(e => e.GetProperty("provider").GetString() == "Outlook");
        Assert.False(outlook.GetProperty("connected").GetBoolean());
        Assert.True(outlook.GetProperty("hasError").GetBoolean());
        Assert.Equal("consent_denied", outlook.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Callback_with_a_consent_denied_error_records_a_persistent_error_without_ever_connecting()
    {
        var (factory, client, personId) = await CreateAuthenticatedContextAsync(allowAutoRedirect: false);
        using var f = factory;
        using var c = client;

        var state = await BuildValidStateAsync(factory, personId);

        var response = await client.GetAsync($"/api/calendar-connections/google/callback?error=access_denied&state={Uri.EscapeDataString(state)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("error=consent_denied", response.Headers.Location!.ToString());

        var statusResponse = await client.GetFromJsonAsync<JsonElement>("/api/calendar-connections");
        var google = statusResponse.EnumerateArray().First(e => e.GetProperty("provider").GetString() == "Google");
        Assert.False(google.GetProperty("connected").GetBoolean());
        Assert.True(google.GetProperty("hasError").GetBoolean());
        Assert.Equal("consent_denied", google.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Callback_rejects_a_missing_or_invalid_state_without_persisting_anything()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync(allowAutoRedirect: false);
        using var f = factory;
        using var c = client;

        var response = await client.GetAsync("/api/calendar-connections/google/callback?code=irrelevant&state=not-a-real-protected-value");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("error=invalid_state", response.Headers.Location!.ToString());

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await dbContext.CalendarConnections.CountAsync());
    }

    [Fact]
    public async Task Two_synced_appointments_for_the_same_person_and_provider_cannot_share_a_provider_event_id()
    {
        // Regression: Story 2.1's migration must not have disturbed the AD-7 partial unique index
        // introduced in Story 1.3.
        var (factory, _, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        dbContext.Appointments.Add(new Appointment(Guid.NewGuid(), personId, "First", now, now.AddMinutes(30), "Google", "dup-event"));
        await dbContext.SaveChangesAsync();

        dbContext.Appointments.Add(new Appointment(Guid.NewGuid(), personId, "Second", now.AddHours(2), now.AddHours(3), "Google", "dup-event"));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    private static async Task<string> BuildValidStateAsync(TestApiFactory factory, Guid personId)
    {
        using var scope = factory.Services.CreateScope();
        var dataProtectionProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var protector = dataProtectionProvider.CreateProtector("CalendarConnectionOAuthState.v1");
        return protector.Protect($"{personId:N}|{DateTimeOffset.UtcNow:O}");
    }
}
