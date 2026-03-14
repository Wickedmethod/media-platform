using System.Reflection;
using Xunit;

namespace MediaPlatform.UnitTests;

/// <summary>
/// Enforces Clean Architecture dependency rules:
///   Domain → (no project references)
///   Application → Domain only
///   Infrastructure → Domain + Application only
///   Api → anything
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Entities.QueueItem).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.Abstractions.IQueueRepository).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.Redis.RedisQueueRepository).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Api.Endpoints.PlayerEndpoints).Assembly;

    [Fact]
    public void Domain_ShouldNotReference_Application()
    {
        AssertNoReference(DomainAssembly, ApplicationAssembly, "Domain must not reference Application");
    }

    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        AssertNoReference(DomainAssembly, InfrastructureAssembly, "Domain must not reference Infrastructure");
    }

    [Fact]
    public void Domain_ShouldNotReference_Api()
    {
        AssertNoReference(DomainAssembly, ApiAssembly, "Domain must not reference Api");
    }

    [Fact]
    public void Application_ShouldNotReference_Infrastructure()
    {
        AssertNoReference(ApplicationAssembly, InfrastructureAssembly, "Application must not reference Infrastructure");
    }

    [Fact]
    public void Application_ShouldNotReference_Api()
    {
        AssertNoReference(ApplicationAssembly, ApiAssembly, "Application must not reference Api");
    }

    [Fact]
    public void Infrastructure_ShouldNotReference_Api()
    {
        AssertNoReference(InfrastructureAssembly, ApiAssembly, "Infrastructure must not reference Api");
    }

    [Fact]
    public void Application_ShouldReference_Domain()
    {
        AssertHasReference(ApplicationAssembly, DomainAssembly, "Application should reference Domain");
    }

    [Fact]
    public void Infrastructure_ShouldReference_Application()
    {
        AssertHasReference(InfrastructureAssembly, ApplicationAssembly, "Infrastructure should reference Application");
    }

    private static void AssertNoReference(Assembly source, Assembly forbidden, string message)
    {
        var references = source.GetReferencedAssemblies().Select(a => a.Name).ToHashSet();
        Assert.False(references.Contains(forbidden.GetName().Name), message);
    }

    private static void AssertHasReference(Assembly source, Assembly expected, string message)
    {
        var references = source.GetReferencedAssemblies().Select(a => a.Name).ToHashSet();
        Assert.True(references.Contains(expected.GetName().Name), message);
    }
}
