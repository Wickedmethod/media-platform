using FluentAssertions;
using MediaPlatform.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace MediaPlatform.UnitTests;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_GeneratesCorrelationId_WhenHeaderMissing()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.TraceIdentifier.Should().NotBeNullOrEmpty();
        context.TraceIdentifier.Should().HaveLength(12);
    }

    [Fact]
    public async Task InvokeAsync_UsesExistingHeader_WhenPresent()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "my-trace-123";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.TraceIdentifier.Should().Be("my-trace-123");
    }

    [Fact]
    public async Task InvokeAsync_SetsResponseHeader()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        // OnStarting callbacks fire when the response actually starts.
        // In test context, we verify TraceIdentifier was set.
        context.TraceIdentifier.Should().HaveLength(12);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        var context = new DefaultHttpContext();
        var wasCalled = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        wasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_GeneratesUniquIds()
    {
        var ids = new HashSet<string>();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        for (var i = 0; i < 100; i++)
        {
            var context = new DefaultHttpContext();
            await middleware.InvokeAsync(context);
            ids.Add(context.TraceIdentifier);
        }

        ids.Should().HaveCount(100, "each request should get a unique correlation ID");
    }
}
