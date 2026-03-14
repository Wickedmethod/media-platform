using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MediaPlatform.Api.Authorization;
using MediaPlatform.Api.Endpoints;
using MediaPlatform.Api.Middleware;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Application.Commands;
using MediaPlatform.Application.Queries;
using MediaPlatform.Infrastructure.Alerting;
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

// CORS (MEDIA-713)
builder.Services.AddCors(options =>
{
    options.AddPolicy("MediaPlatform", policy =>
    {
        var origins = new List<string> { "http://localhost:5173", "http://localhost:3000" };
        var configOrigins = builder.Configuration.GetValue<string>("Cors:AllowedOrigins");
        if (!string.IsNullOrWhiteSpace(configOrigins))
        {
            origins.AddRange(configOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        policy
            .WithOrigins(origins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Redis
var redisConnection = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));

// Health checks
builder.Services.AddHealthChecks()
    .AddRedis(redisConnection, name: "redis", tags: ["ready"]);

// Authentication — dual scheme: JWT Bearer + Worker Key (MEDIA-713)
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

            // Support SSE token via query param (EventSource can't send headers)
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var token = context.Request.Query["token"];
                    if (!string.IsNullOrEmpty(token))
                        context.Token = token;
                    return Task.CompletedTask;
                }
            };
        })
        .AddScheme<AuthenticationSchemeOptions, WorkerKeyAuthenticationHandler>(
            WorkerKeyAuthenticationHandler.SchemeName, null);

    builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();
}
else
{
    // Development mode — auto-authenticate all requests as admin
    builder.Services.AddAuthentication("Development")
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>("Development", null)
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
            JwtBearerDefaults.AuthenticationScheme, null)
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
            WorkerKeyAuthenticationHandler.SchemeName, null);
}

// Authorization policies (MEDIA-713)
// All policies include both auth schemes so authenticated users get 403 (not 401)
builder.Services.AddAuthorization(options =>
{
    // Read-only: JWT users or Worker Key
    options.AddPolicy(AuthPolicies.ReadAccess, policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, WorkerKeyAuthenticationHandler.SchemeName)
              .RequireAuthenticatedUser());

    // Queue add: JWT users or Worker Key
    options.AddPolicy(AuthPolicies.QueueAdd, policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, WorkerKeyAuthenticationHandler.SchemeName)
              .RequireAuthenticatedUser());

    // Own items: JWT user (Worker Key users are rejected — TV cannot delete)
    options.AddPolicy(AuthPolicies.QueueOwner, policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, WorkerKeyAuthenticationHandler.SchemeName)
              .RequireAuthenticatedUser()
              .RequireAssertion(ctx => !ctx.User.HasClaim("origin", "worker")));

    // TV reporting: must have worker role
    options.AddPolicy(AuthPolicies.WorkerOnly, policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, WorkerKeyAuthenticationHandler.SchemeName)
              .RequireAuthenticatedUser()
              .RequireRole("worker"));

    // Admin actions: JWT with media-admin role
    options.AddPolicy(AuthPolicies.AdminOnly, policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, WorkerKeyAuthenticationHandler.SchemeName)
              .RequireAuthenticatedUser()
              .RequireRole(MediaPlatformRoles.Admin));

    // Legacy policies
    options.AddPolicy(AuthPolicies.OperatorOrAdmin, p => p.RequireRole(MediaPlatformRoles.Admin, MediaPlatformRoles.Operator));
    options.AddPolicy(AuthPolicies.ViewerOrAbove, p => p.RequireRole(MediaPlatformRoles.Admin, MediaPlatformRoles.Operator, MediaPlatformRoles.Viewer));
});

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
    options.AddFixedWindowLimiter("queue-add-tv", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

// Infrastructure
builder.Services.AddScoped<IQueueRepository, RedisQueueRepository>();
builder.Services.AddScoped<IPlayerRegistry, RedisPlayerRegistry>();
builder.Services.AddScoped<IPlayerLogStore, RedisPlayerLogStore>();
builder.Services.AddScoped<INetworkMetricsStore, RedisNetworkMetricsStore>();
builder.Services.AddSingleton<IEventBroadcaster, InMemoryEventBroadcaster>();
builder.Services.AddSingleton<INotificationService, WebhookNotificationService>();
builder.Services.AddSingleton<IAnalyticsTracker, InMemoryAnalyticsTracker>();
builder.Services.AddSingleton<IAuditLog, InMemoryAuditLog>();
builder.Services.AddSingleton<IKillSwitch, InMemoryKillSwitch>();
builder.Services.AddSingleton<IAnomalyDetector, SlidingWindowAnomalyDetector>();
builder.Services.AddSingleton<IPolicyEngine, InMemoryPolicyEngine>();
builder.Services.AddSingleton<MediaPlatformMetrics>();
builder.Services.AddHttpClient("webhooks");
builder.Services.AddHttpClient("alerts");

// Alerting (MEDIA-743)
builder.Services.Configure<AlertingOptions>(builder.Configuration.GetSection("Alerting"));
builder.Services.AddSingleton<IAlertDispatcher, AlertDispatcher>();
builder.Services.AddHostedService<AnomalyAlertService>();

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

app.UseCors("MediaPlatform");
app.UseDefaultFiles();
app.UseStaticFiles();

// Correlation ID must be first so all downstream middleware has tracing context
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpMetrics();

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
app.MapDiagnosticsEndpoints();
app.MapMetrics();

app.Run();

// Marker class for WebApplicationFactory<Program>
public partial class Program;
