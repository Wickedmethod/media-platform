using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StackExchange.Redis;

namespace MediaPlatform.IntegrationTests;

/// <summary>
/// Test host that replaces Redis with an in-memory stub so contract tests
/// run without any infrastructure dependency.
/// </summary>
public class MediaPlatformFactory : WebApplicationFactory<Program>
{
    public IQueueRepository QueueRepository { get; } = CreateDefaultQueueRepo();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real Redis registrations
            RemoveService<IConnectionMultiplexer>(services);
            RemoveService<IQueueRepository>(services);

            // Remove Redis health check
            var healthDescriptor = services.FirstOrDefault(d =>
                d.ServiceType.Name.Contains("HealthCheckService", StringComparison.OrdinalIgnoreCase));

            // Register stubs
            services.AddScoped(_ => QueueRepository);
            services.AddSingleton(Substitute.For<IConnectionMultiplexer>());

            // Override health checks to avoid Redis probe
            services.AddHealthChecks();
        });
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in descriptors) services.Remove(d);
    }

    private static IQueueRepository CreateDefaultQueueRepo()
    {
        var repo = Substitute.For<IQueueRepository>();
        repo.GetQueueAsync(Arg.Any<CancellationToken>()).Returns([]);
        repo.GetPlaybackStateAsync(Arg.Any<CancellationToken>())
            .Returns(new PlaybackState());
        repo.GetQueueModeAsync(Arg.Any<CancellationToken>())
            .Returns(QueueMode.Normal);
        return repo;
    }
}
