using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MediaPlatform.Api.Authorization;
using MediaPlatform.Api.Endpoints;
using MediaPlatform.Api.Middleware;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Application.Commands;
using MediaPlatform.Application.Queries;
using MediaPlatform.Infrastructure.Analytics;
using MediaPlatform.Infrastructure.Events;
using MediaPlatform.Infrastructure.Metrics;
using MediaPlatform.Infrastructure.Notifications;
using MediaPlatform.Infrastructure.Redis;
using MediaPlatform.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Prometheus;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Structured logging (MEDIA-741)
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MediaPlatform")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"));

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

// Authentication — Keycloak JWT Bearer (active when Authority is configured)
var keycloakAuthority = builder.Configuration.GetValue<string>("Keycloak:Authority");
if (!string.IsNullOrEmpty(keycloakAuthority))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = keycloakAuthority;
            options.Audience = builder.Configuration.GetValue<string>("Keycloak:Audience") ?? "account";
            options.RequireHttpsMetadata = builder.Configuration.GetValue("Keycloak:RequireHttpsMetadata", false);
            options.TokenValidationParameters.ValidIssuer = keycloakAuthority;
        });
    builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthPolicies.AdminOnly, p => p.RequireRole(MediaPlatformRoles.Admin));
        options.AddPolicy(AuthPolicies.OperatorOrAdmin, p => p.RequireRole(MediaPlatformRoles.Admin, MediaPlatformRoles.Operator));
        options.AddPolicy(AuthPolicies.ViewerOrAbove, p => p.RequireRole(MediaPlatformRoles.Admin, MediaPlatformRoles.Operator, MediaPlatformRoles.Viewer));
    });
}
else
{
    // No Keycloak configured — allow anonymous (development mode)
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();
}

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("commands", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("general", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

// Infrastructure
builder.Services.AddScoped<IQueueRepository, RedisQueueRepository>();
builder.Services.AddScoped<IPlayerRegistry, RedisPlayerRegistry>();
builder.Services.AddSingleton<IEventBroadcaster, InMemoryEventBroadcaster>();
builder.Services.AddSingleton<INotificationService, WebhookNotificationService>();
builder.Services.AddSingleton<IAnalyticsTracker, InMemoryAnalyticsTracker>();
builder.Services.AddSingleton<IAuditLog, InMemoryAuditLog>();
builder.Services.AddSingleton<IKillSwitch, InMemoryKillSwitch>();
builder.Services.AddSingleton<IAnomalyDetector, SlidingWindowAnomalyDetector>();
builder.Services.AddSingleton<IPolicyEngine, InMemoryPolicyEngine>();
builder.Services.AddSingleton<MediaPlatformMetrics>();
builder.Services.AddHttpClient("webhooks");

// OpenAPI spec generation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, ctx, ct) =>
    {
        doc.Info = new()
        {
            Title = "Media Platform API",
            Version = "v1",
            Description = "Queue-based media playback controller for homelab"
        };
        return Task.CompletedTask;
    });
});

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

// Correlation ID must be first so all downstream middleware has tracing context
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpMetrics();

// Security middleware pipeline (order matters)
app.UseMiddleware<WorkerAuthMiddleware>();
app.UseAuthentication();
app.UseMiddleware<KillSwitchMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();

// Health endpoints (no auth required)
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // no checks — just "am I alive?"
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// OpenAPI + Scalar (dev mode)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapQueueEndpoints();
app.MapPlayerEndpoints();
app.MapSyncEndpoints();
app.MapEventStreamEndpoints();
app.MapNotificationEndpoints();
app.MapAnalyticsEndpoints();
app.MapAdminEndpoints();
app.MapPolicyEndpoints();
app.MapWorkerEndpoints();
app.MapMetrics();

app.Run();

// Marker class for WebApplicationFactory<Program>
public partial class Program;
