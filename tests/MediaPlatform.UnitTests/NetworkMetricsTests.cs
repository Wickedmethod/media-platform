using MediaPlatform.Application.Abstractions;
using MediaPlatform.Api.Endpoints;
using Xunit;

namespace MediaPlatform.UnitTests;

public class NetworkMetricsTests
{
    [Fact]
    public void NetworkMetrics_ConstructsCorrectly()
    {
        var metrics = new NetworkMetrics(
            "pi-living-room",
            "2026-03-14T10:30:00Z",
            new LatencyMetrics(12, 8, 45, 28, 4, 0),
            new DnsMetrics(3, 0),
            new BandwidthMetrics(85.2, "2026-03-14T10:25:00Z"));

        Assert.Equal("pi-living-room", metrics.PlayerId);
        Assert.Equal(12, metrics.Latency.AvgMs);
        Assert.Equal(3, metrics.Dns.AvgResolveMs);
        Assert.Equal(85.2, metrics.Bandwidth.LastMbps);
    }

    [Fact]
    public void NetworkTrend_StableDefaults()
    {
        var trend = new NetworkTrend("stable", 14, "stable", 82.5);

        Assert.Equal("stable", trend.LatencyTrend);
        Assert.Equal(14, trend.AvgLatency1h);
        Assert.Equal("stable", trend.BandwidthTrend);
        Assert.Equal(82.5, trend.AvgBandwidth1h);
    }

    [Fact]
    public void SubmitNetworkMetricsRequest_HasExpectedShape()
    {
        var request = new SubmitNetworkMetricsRequest(
            "pi-1",
            "2026-03-14T10:00:00Z",
            new LatencyMetricsDto(15, 5, 50, 30, 4, 0),
            new DnsMetricsDto(5, 0),
            new BandwidthMetricsDto(50.0, "2026-03-14T09:55:00Z"));

        Assert.Equal("pi-1", request.PlayerId);
        Assert.Equal(15, request.Latency.AvgMs);
    }

    [Fact]
    public void NetworkMetricsResponse_NullCurrent_IsValid()
    {
        var response = new NetworkMetricsResponse(
            null,
            new NetworkTrendDto("stable", 0, "stable", 0));

        Assert.Null(response.Current);
        Assert.Equal("stable", response.Trend.LatencyTrend);
    }

    [Fact]
    public void NetworkMetricsCurrentDto_HasAllFields()
    {
        var dto = new NetworkMetricsCurrentDto(
            "pi-2", "2026-03-14T10:00:00Z",
            new LatencyMetricsDto(10, 5, 20, 15, 4, 1),
            new DnsMetricsDto(2, 0),
            new BandwidthMetricsDto(100.0, "2026-03-14T09:55:00Z"));

        Assert.Equal("pi-2", dto.PlayerId);
        Assert.Equal(1, dto.Latency.Failures);
        Assert.Equal(100.0, dto.Bandwidth.LastMbps);
    }
}
