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

// Infrastructure
builder.Services.AddScoped<IQueueRepository, RedisQueueRepository>();

// Application handlers
builder.Services.AddScoped<AddToQueueHandler>();
builder.Services.AddScoped<RemoveFromQueueHandler>();
builder.Services.AddScoped<PlayerCommandHandler>();
builder.Services.AddScoped<GetQueueHandler>();
builder.Services.AddScoped<GetPlaybackStateHandler>();

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapQueueEndpoints();
app.MapPlayerEndpoints();

app.Run();
