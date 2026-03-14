using MediaPlatform.Api.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MediaPlatform.IntegrationTests;

/// <summary>
/// Test factory that uses real auth handlers (WorkerKey + stub Bearer)
/// instead of the DevelopmentAuthenticationHandler, to verify access policies.
/// </summary>
public class AuthTestFactory : MediaPlatformFactory
{
    public const string WorkerKey = "test-worker-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set Worker:ApiKey so the WorkerKeyAuthenticationHandler can verify it
        builder.UseSetting("Worker:ApiKey", WorkerKey);

        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            // Remove all authentication registrations from Program.cs (DevelopmentAuthHandler)
            RemoveAllOf<IAuthenticationService>(services);
            RemoveAllOf<IClaimsTransformation>(services);
            RemoveAllOf<IAuthenticationHandlerProvider>(services);
            RemoveAllOf<IAuthenticationSchemeProvider>(services);
            var configDescriptors = services.Where(d =>
                d.ServiceType == typeof(IConfigureOptions<AuthenticationOptions>)).ToList();
            foreach (var d in configDescriptors) services.Remove(d);

            // Re-register: real WorkerKey handler + stub Bearer (always NoResult)
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, WorkerKeyAuthenticationHandler>(
                    WorkerKeyAuthenticationHandler.SchemeName, null)
                .AddScheme<AuthenticationSchemeOptions, StubBearerAuthenticationHandler>(
                    JwtBearerDefaults.AuthenticationScheme, null);
        });
    }

    private static void RemoveAllOf<T>(IServiceCollection services)
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in descriptors) services.Remove(d);
    }
}
