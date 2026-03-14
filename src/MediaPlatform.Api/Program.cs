using MediaPlatform.Api.Endpoints;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Application.Commands;
using MediaPlatform.Application.Queries;
using MediaPlatform.Infrastructure.Redis;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// CORS for local development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Redis
var redisConnection = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));

// Health checks
builder.Services.AddHealthChecks()
    .AddRedis(redisConnection, name: "redis", tags: ["ready"]);

// Infrastructure
builder.Services.AddScoped<IQueueRepository, RedisQueueRepository>();

// Application handlers
builder.Services.AddScoped<AddToQueueHandler>();
builder.Services.AddScoped<RemoveFromQueueHandler>();
builder.Services.AddScoped<PlayerCommandHandler>();
builder.Services.AddScoped<GetQueueHandler>();
builder.Services.AddScoped<GetPlaybackStateHandler>();
builder.Services.AddScoped<ReportPositionHandler>();
builder.Services.AddScoped<ReportErrorHandler>();
builder.Services.AddScoped<SetQueueModeHandler>();

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// Health endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // no checks — just "am I alive?"
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapQueueEndpoints();
app.MapPlayerEndpoints();

app.Run();
