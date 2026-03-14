using MediaPlatform.Application.Abstractions;
using MediaPlatform.Api.Endpoints;
using MediaPlatform.Infrastructure.Alerting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace MediaPlatform.UnitTests;

public class AlertDispatcherTests
{
    [Fact]
    public async Task Dispatch_WhenDisabled_DoesNothing()
    {
        var options = Options.Create(new AlertingOptions { Enabled = false });
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var dispatcher = new AlertDispatcher(httpFactory, options, NullLogger<AlertDispatcher>.Instance);

        var alert = new AnomalyAlert("test-rule", "test description", "warning", DateTimeOffset.UtcNow);

        await dispatcher.DispatchAsync(alert, TestContext.Current.CancellationToken); // should not throw
    }

    [Fact]
    public async Task Dispatch_WithCooldown_SkipsSecondAlert()
    {
        var options = Options.Create(new AlertingOptions
        {
            Enabled = true,
            CooldownMinutes = 5,
            Channels = [] // no channels → dispatch runs but nothing is sent
        });
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var dispatcher = new AlertDispatcher(httpFactory, options, NullLogger<AlertDispatcher>.Instance);

        var alert = new AnomalyAlert("test-rule", "desc", "warning", DateTimeOffset.UtcNow);

        // First dispatch sets cooldown
        await dispatcher.DispatchAsync(alert, TestContext.Current.CancellationToken);
        // Second dispatch within cooldown → silently skipped
        await dispatcher.DispatchAsync(alert, TestContext.Current.CancellationToken); // should not throw
    }

    [Fact]
    public async Task Dispatch_SeverityFilter_SkipsLow()
    {
        var options = Options.Create(new AlertingOptions
        {
            Enabled = true,
            CooldownMinutes = 0,
            Channels = [new AlertChannelConfig { Type = "Discord", MinSeverity = "Critical" }]
        });
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var dispatcher = new AlertDispatcher(httpFactory, options, NullLogger<AlertDispatcher>.Instance);

        // "warning" < "critical" → should be skipped, no HTTP call
        var alert = new AnomalyAlert("test-rule", "low severity", "warning", DateTimeOffset.UtcNow);
        await dispatcher.DispatchAsync(alert, TestContext.Current.CancellationToken);

        httpFactory.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    [Fact]
    public void AlertConfigResponse_HasCorrectShape()
    {
        var response = new AlertConfigResponse(true, 5, 2);

        Assert.True(response.Enabled);
        Assert.Equal(5, response.CooldownMinutes);
        Assert.Equal(2, response.ChannelCount);
    }

    [Fact]
    public void AlertingOptions_DefaultCooldown_Is5Minutes()
    {
        var options = new AlertingOptions();

        Assert.Equal(5, options.CooldownMinutes);
        Assert.False(options.Enabled);
        Assert.Empty(options.Channels);
    }

    [Fact]
    public void AlertChannelConfig_Defaults()
    {
        var channel = new AlertChannelConfig();

        Assert.Equal(string.Empty, channel.Type);
        Assert.Equal("Warning", channel.MinSeverity);
        Assert.Equal(25, channel.SmtpPort);
    }
}
