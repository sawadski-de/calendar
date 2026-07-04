using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real Api host (migrations + admin bootstrap run exactly as in production) against a
/// freshly created, uniquely named database inside the shared <see cref="PostgresContainerFixture"/>
/// container — isolates every test class without paying container-startup cost more than once.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin#12345";

    private readonly string _connectionString;

    public TestApiFactory(string containerConnectionString)
    {
        var databaseName = $"test_{Guid.NewGuid():N}";
        CreateDatabase(containerConnectionString, databaseName);

        var builder = new NpgsqlConnectionStringBuilder(containerConnectionString) { Database = databaseName };
        _connectionString = builder.ConnectionString;
    }

    public HttpClient CreateHttpsClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    private static void CreateDatabase(string adminConnectionString, string databaseName)
    {
        using var connection = new NpgsqlConnection(adminConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        command.ExecuteNonQuery();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString,
                ["INITIAL_ADMIN_EMAIL"] = AdminEmail,
                ["INITIAL_ADMIN_PASSWORD"] = AdminPassword,
                // Story 2.1: AesGcmTokenEncryption fails fast without a valid 32-byte base64 key —
                // every test run gets its own random key, tokens never need to survive across runs.
                ["TOKEN_ENCRYPTION_KEY"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                ["GOOGLE_OAUTH_CLIENT_ID"] = "test-client-id",
                ["GOOGLE_OAUTH_CLIENT_SECRET"] = "test-client-secret",
                ["GOOGLE_OAUTH_REDIRECT_URI"] = "https://localhost/api/calendar-connections/google/callback",
                ["MICROSOFT_OAUTH_CLIENT_ID"] = "test-client-id",
                ["MICROSOFT_OAUTH_CLIENT_SECRET"] = "test-client-secret",
                ["MICROSOFT_OAUTH_REDIRECT_URI"] = "https://localhost/api/calendar-connections/outlook/callback",
                ["MICROSOFT_OAUTH_TENANT_ID"] = "common",
            });
        });
    }
}
