using Application.Accounts;
using Domain;

namespace Api.Startup;

/// <summary>
/// Story 1.1, AC 10: creates exactly one admin from env vars on first-ever startup, so a
/// chicken-and-egg problem (no admin can create accounts because no admin exists) cannot occur.
/// Idempotent — a no-op once at least one admin already exists.
/// </summary>
public static class AdminBootstrap
{
    public static async Task EnsureInitialAdminAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var personRepository = services.GetRequiredService<IPersonRepository>();
        if (await personRepository.CountAdminsAsync() > 0)
        {
            return;
        }

        var adminEmail = configuration["INITIAL_ADMIN_EMAIL"];
        var adminPassword = configuration["INITIAL_ADMIN_PASSWORD"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "No admin account exists and INITIAL_ADMIN_EMAIL/INITIAL_ADMIN_PASSWORD are not set — cannot bootstrap.");
        }

        var provisioning = services.GetRequiredService<IAccountProvisioningService>();
        var result = await provisioning.ProvisionAsync(adminEmail, adminPassword, PersonRole.Admin);
        if (!result.Succeeded)
        {
            // If the Api ever runs as more than one instance, two replicas can both observe zero
            // admins before either commits. A duplicate-email failure here means the other replica
            // already won the race — that's the intended outcome, not a startup failure.
            if (result.Errors.Contains("DuplicateEmail") || result.Errors.Contains("DuplicateUserName"))
            {
                logger.LogInformation(
                    "Initial admin bootstrap skipped — {Email} was already provisioned (likely by a concurrent instance).",
                    adminEmail);
                return;
            }

            throw new InvalidOperationException(
                $"Failed to bootstrap initial admin account: {string.Join(", ", result.Errors)}");
        }

        logger.LogInformation("Bootstrapped initial admin account {Email}", adminEmail);
    }
}
