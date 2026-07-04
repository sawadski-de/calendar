using Application.Accounts;
using Application.Appointments;
using Application.Sync;
using Infrastructure.Accounts;
using Infrastructure.Appointments;
using Infrastructure.Persistence;
using Infrastructure.Sync;
using Microsoft.EntityFrameworkCore;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"), npgsql => npgsql.EnableRetryOnFailure())
        // Mirrors Api/Program.cs exactly (AD-11: Worker and Api share the same schema/DbContext) — a
        // divergence here would silently read/write the wrong columns since EFCore.NamingConventions
        // changes every generated SQL identifier.
        .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton(TimeProvider.System);
// Factory-based so IConfiguration is read lazily at first resolution, not at this top-level
// statement's execution time (see Api/Program.cs for why that distinction matters).
builder.Services.AddSingleton(sp => new TokenEncryptionOptions
{
    Base64Key = sp.GetRequiredService<IConfiguration>()["TOKEN_ENCRYPTION_KEY"] ?? string.Empty,
});
builder.Services.AddSingleton<ITokenEncryption, AesGcmTokenEncryption>();

builder.Services.Configure<GoogleOAuthOptions>(options =>
{
    options.ClientId = builder.Configuration["GOOGLE_OAUTH_CLIENT_ID"] ?? string.Empty;
    options.ClientSecret = builder.Configuration["GOOGLE_OAUTH_CLIENT_SECRET"] ?? string.Empty;
    options.RedirectUri = builder.Configuration["GOOGLE_OAUTH_REDIRECT_URI"] ?? string.Empty;
});
builder.Services.AddHttpClient<GoogleOAuthClient>();
builder.Services.AddHttpClient<GoogleCalendarProvider>();

builder.Services.Configure<MicrosoftOAuthOptions>(options =>
{
    options.ClientId = builder.Configuration["MICROSOFT_OAUTH_CLIENT_ID"] ?? string.Empty;
    options.ClientSecret = builder.Configuration["MICROSOFT_OAUTH_CLIENT_SECRET"] ?? string.Empty;
    options.RedirectUri = builder.Configuration["MICROSOFT_OAUTH_REDIRECT_URI"] ?? string.Empty;
    options.TenantId = builder.Configuration["MICROSOFT_OAUTH_TENANT_ID"] ?? "common";
});
builder.Services.AddHttpClient<MicrosoftOAuthClient>();
builder.Services.AddHttpClient<OutlookCalendarProvider>();

// Two same-typed (ICalendarProvider) dependencies can't be positionally resolved by the container —
// an explicit factory picks the concrete Google/Outlook instance for each resolver parameter.
builder.Services.AddScoped<ICalendarProviderResolver>(sp => new CalendarProviderResolver(
    sp.GetRequiredService<GoogleCalendarProvider>(),
    sp.GetRequiredService<OutlookCalendarProvider>()));

builder.Services.AddScoped<ICalendarConnectionRepository, CalendarConnectionRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<CalendarSyncService>();

builder.Services.AddHostedService<SyncBackgroundService>();

var host = builder.Build();
host.Run();
