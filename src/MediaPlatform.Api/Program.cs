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
using MediaPlatform.Infrastructure.Notifications;
using MediaPlatform.Infrastructure.Redis;
using MediaPlatform.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
builder.Services.AddSingleton<IEventBroadcaster, InMemoryEventBroadcaster>();
builder.Services.AddSingleton<INotificationService, WebhookNotificationService>();
builder.Services.AddSingleton<IAnalyticsTracker, InMemoryAnalyticsTracker>();
builder.Services.AddSingleton<IAuditLog, InMemoryAuditLog>();
builder.Services.AddSingleton<IKillSwitch, InMemoryKillSwitch>();
builder.Services.AddSingleton<IAnomalyDetector, SlidingWindowAnomalyDetector>();
builder.Services.AddHttpClient("webhooks");

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

// Security middleware pipeline (order matters)
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

app.MapQueueEndpoints();
app.MapPlayerEndpoints();
app.MapEventStreamEndpoints();
app.MapNotificationEndpoints();
app.MapAnalyticsEndpoints();
app.MapAdminEndpoints();

app.Run();

// Marker class for WebApplicationFactory<Program>
public partial class Program;
