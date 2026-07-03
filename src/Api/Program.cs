using Api.Authorization;
using Api.Endpoints;
using Api.Errors;
using Api.Startup;
using Application.Accounts;
using Application.Appointments;
using Infrastructure.Accounts;
using Infrastructure.Appointments;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options
        // EnableRetryOnFailure gives RoleChangeService's Serializable transaction (see
        // PersonRepository.ExecuteAtomicallyAsync) an execution strategy that automatically retries
        // a transaction Postgres aborted for a genuine write-skew conflict (SQLSTATE 40001).
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"), npgsql => npgsql.EnableRetryOnFailure())
        // Dev Notes call for "snake_case Postgres columns via EF Core default mapping" — plain EF
        // Core actually defaults to quoted PascalCase, so this convention is applied explicitly to
        // make that true rather than leaving the two documents (Dev Notes vs. actual schema) disagree.
        .UseSnakeCaseNamingConvention());

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // No email-confirmation flow exists in this app — leaving Identity's defaults untouched
        // would silently lock every admin-created account out at first login (Story 1.1 guardrail).
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

// AddIdentityCore() does not register an authentication scheme by itself — wire the cookie up
// explicitly (AD-10: HttpOnly/Secure/SameSite=Lax, no bearer token ever reaches client JS).
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.Requirements.Add(new AdminOnlyRequirement()));
builder.Services.AddScoped<IAuthorizationHandler, AdminOnlyAuthorizationHandler>();

builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IAccountProvisioningService, IdentityAccountProvisioningService>();
builder.Services.AddScoped<RoleChangeService>();
builder.Services.AddScoped<IAppointmentViewService, AppointmentViewService>();

builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    // Framework-generated problem+json (e.g. malformed request body / model-binding failures) has no
    // "code" extension by default — backstop it here so AD-13's "stable code field" holds universally,
    // not just for the endpoints that construct ProblemResults themselves.
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions.TryAdd("code", "invalid-request");
});

// Caddy is the only ingress inside the compose network, and the Api's own port is never published
// to the host (see docker-compose.yml) — so only Caddy's container can reach the Api at all. Trust
// forwarded headers specifically from Caddy's resolved address rather than clearing the trusted-proxy
// list entirely, which would accept X-Forwarded-For/-Proto from any source that did reach the Api.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    try
    {
        foreach (var address in System.Net.Dns.GetHostAddresses("caddy"))
        {
            options.KnownProxies.Add(address);
        }
    }
    catch (System.Net.Sockets.SocketException)
    {
        // Not running behind the compose network (e.g. WebApplicationFactory-based tests, or
        // `dotnet run` outside Docker) — no proxy to trust, forwarded headers are simply ignored.
    }
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await AdminBootstrap.EnsureInitialAdminAsync(scope.ServiceProvider, app.Configuration, app.Logger);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));
app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapAppointmentEndpoints();

app.Run();

// Exposed for WebApplicationFactory<Program>-based integration tests (Task 5).
public partial class Program;
