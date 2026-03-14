using FluentAssertions;
using MediaPlatform.Api.Endpoints;
using MediaPlatform.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace MediaPlatform.UnitTests;

public class QueueConsistencyGuardTests
{
    private readonly IQueueRepository _repo = Substitute.For<IQueueRepository>();

    [Fact]
    public async Task CheckVersionConflict_NoHeader_ReturnsNull()
    {
        var http = CreateHttpContext();

        var result = await QueueEndpoints.CheckVersionConflict(http, _repo, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckVersionConflict_MatchingVersion_ReturnsNull()
    {
        _repo.GetVersionAsync(Arg.Any<CancellationToken>()).Returns(42L);
        var http = CreateHttpContext(queueVersion: "42");

        var result = await QueueEndpoints.CheckVersionConflict(http, _repo, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckVersionConflict_StaleVersion_Returns409()
    {
        _repo.GetVersionAsync(Arg.Any<CancellationToken>()).Returns(43L);
        var http = CreateHttpContext(queueVersion: "42");

        var result = await QueueEndpoints.CheckVersionConflict(http, _repo, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckVersionConflict_InvalidHeader_ReturnsNull()
    {
        var http = CreateHttpContext(queueVersion: "not-a-number");

        var result = await QueueEndpoints.CheckVersionConflict(http, _repo, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckVersionConflict_VersionZero_MatchesDefault()
    {
        _repo.GetVersionAsync(Arg.Any<CancellationToken>()).Returns(0L);
        var http = CreateHttpContext(queueVersion: "0");

        var result = await QueueEndpoints.CheckVersionConflict(http, _repo, CancellationToken.None);

        result.Should().BeNull();
    }

    private static HttpContext CreateHttpContext(string? queueVersion = null)
    {
        var context = new DefaultHttpContext();
        if (queueVersion is not null)
        {
            context.Request.Headers["X-Queue-Version"] = queueVersion;
        }
        return context;
    }
}
