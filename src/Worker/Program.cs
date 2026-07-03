using Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<SyncBackgroundService>();

var host = builder.Build();
host.Run();
