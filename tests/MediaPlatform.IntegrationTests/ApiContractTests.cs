using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MediaPlatform.Application.Abstractions;
using NSubstitute;
using Xunit;

namespace MediaPlatform.IntegrationTests;

/// <summary>
/// API contract tests validating response shapes, status codes, content types,
/// and error contract behavior. These tests run against the real API pipeline
/// (minus Redis) and enforce the contract surface that any consumer depends on.
/// </summary>
public class ApiContractTests : IClassFixture<MediaPlatformFactory>
{
    private readonly HttpClient _client;
    private readonly MediaPlatformFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ApiContractTests(MediaPlatformFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Health Endpoints ──────────────────────────────────────

    [Fact]
    public async Task Health_Live_Returns200()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_Root_Returns200_WithStatusField()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("status", out var status));
        Assert.Equal("healthy", status.GetString());
    }

    // ── Queue Endpoints ───────────────────────────────────────

    [Fact]
    public async Task Queue_Get_ReturnsArray()
    {
        var response = await _client.GetAsync("/queue");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var arr = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
    }

    [Fact]
    public async Task Queue_Add_InvalidUrl_ReturnsBadRequest_WithApiError()
    {
        var response = await _client.PostAsJsonAsync("/queue/add",
            new { url = "not-a-url", title = "test" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("error", out _), "BadRequest response must have 'error' field");
    }

    [Fact]
    public async Task Queue_Mode_Get_ReturnsMode()
    {
        var response = await _client.GetAsync("/queue/mode");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("mode", out var mode));
        var modeValue = mode.GetString();
        Assert.True(modeValue is "Normal" or "Shuffle" or "PlayNext", $"Unexpected mode: {modeValue}");
    }

    [Fact]
    public async Task Queue_Mode_InvalidMode_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/queue/mode", new { mode = "InvalidMode" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("error", out _));
    }

    // ── Player Endpoints ──────────────────────────────────────

    [Fact]
    public async Task NowPlaying_ReturnsPlaybackStateShape()
    {
        var response = await _client.GetAsync("/now-playing");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Required fields in PlaybackStateResponse
        Assert.True(body.TryGetProperty("state", out _), "Response must have 'state'");
        Assert.True(body.TryGetProperty("positionSeconds", out _), "Response must have 'positionSeconds'");
        Assert.True(body.TryGetProperty("retryCount", out _), "Response must have 'retryCount'");
    }

    [Fact]
    public async Task Player_Play_FromIdle_ReturnsOk_WithState()
    {
        var response = await _client.PostAsync("/player/play", null);

        // Might be OK (if queue has items) or Conflict (invalid transition)
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
            $"Expected OK or Conflict, got {response.StatusCode}");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(body.TryGetProperty("state", out _));
        }
        else
        {
            Assert.True(body.TryGetProperty("error", out _));
        }
    }

    [Fact]
    public async Task Player_InvalidTransition_Returns409_WithApiError()
    {
        // Pause from Idle should be invalid
        var response = await _client.PostAsync("/player/pause", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("error", out var error));
        Assert.False(string.IsNullOrEmpty(error.GetString()));
    }

    [Fact]
    public async Task Player_Position_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/player/position",
            new { positionSeconds = 10.5 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("state", out _));
    }

    [Fact]
    public async Task Player_Error_ReturnsOk_WithState()
    {
        var response = await _client.PostAsJsonAsync("/player/error",
            new { reason = "test error" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("state", out _));
    }

    // ── SSE Endpoint ──────────────────────────────────────────

    [Fact]
    public async Task Events_ReturnsEventStream_ContentType()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/events");
            var response = await _client.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, cts.Token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        }
        catch (OperationCanceledException) { /* expected — SSE stays open */ }
    }

    // ── Webhook Endpoints ─────────────────────────────────────

    [Fact]
    public async Task Webhooks_Get_ReturnsArray()
    {
        var response = await _client.GetAsync("/webhooks");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var arr = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
    }

    [Fact]
    public async Task Webhooks_Register_InvalidUrl_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/webhooks",
            new { id = "test", url = "not-a-url", eventTypes = new[] { "playback-state" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhooks_Register_ValidUrl_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/webhooks",
            new { id = "test-hook", url = "https://example.com/hook", eventTypes = new[] { "playback-state" } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("id", out _));
        Assert.True(body.TryGetProperty("url", out _));
    }

    [Fact]
    public async Task Webhooks_Delete_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync("/webhooks/nonexistent");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── Analytics Endpoints ───────────────────────────────────

    [Fact]
    public async Task Analytics_Get_ReturnsSnapshotShape()
    {
        var response = await _client.GetAsync("/analytics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("totalCommands", out _));
        Assert.True(body.TryGetProperty("totalErrors", out _));
        Assert.True(body.TryGetProperty("totalPlaybackSeconds", out _));
        Assert.True(body.TryGetProperty("averageCommandLatencyMs", out _));
    }

    [Fact]
    public async Task Analytics_Export_ReturnsJson()
    {
        var response = await _client.GetAsync("/analytics/export");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    // ── Static Files ──────────────────────────────────────────

    [Fact]
    public async Task StaticFiles_IndexHtml_Returns200()
    {
        var response = await _client.GetAsync("/index.html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StaticFiles_TvHtml_Returns200()
    {
        var response = await _client.GetAsync("/tv.html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Admin / Security Endpoints ────────────────────────────

    [Fact]
    public async Task KillSwitch_Get_ReturnsStatus()
    {
        var response = await _client.GetAsync("/admin/kill-switch");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("active", out var active));
        Assert.False(active.GetBoolean());
    }

    [Fact]
    public async Task KillSwitch_Activate_BlocksWriteOperations()
    {
        // Activate kill switch
        var activateResponse = await _client.PostAsJsonAsync("/admin/kill-switch", new { reason = "test" });
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        // Verify write operations are blocked
        var playResponse = await _client.PostAsync("/player/play", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, playResponse.StatusCode);

        // Verify GET operations still work
        var getResponse = await _client.GetAsync("/admin/kill-switch");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Deactivate
        var deactivateResponse = await _client.DeleteAsync("/admin/kill-switch");
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        // Verify write operations work again
        var playAgainResponse = await _client.PostAsync("/player/play", null);
        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, playAgainResponse.StatusCode);
    }

    [Fact]
    public async Task Audit_Get_ReturnsArray()
    {
        var response = await _client.GetAsync("/admin/audit");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var arr = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
    }

    [Fact]
    public async Task Anomalies_Get_ReturnsReport()
    {
        var response = await _client.GetAsync("/admin/anomalies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("hasAnomalies", out _));
        Assert.True(body.TryGetProperty("alerts", out _));
    }

    // ── Policy Endpoints ──────────────────────────────────────

    [Fact]
    public async Task Policies_Get_ReturnsArray()
    {
        var response = await _client.GetAsync("/policies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var arr = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
    }

    [Fact]
    public async Task Policies_Add_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/policies",
            new { name = "Test Block", type = "BlockedChannel", value = "blocked123" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("id", out _));
        Assert.True(body.TryGetProperty("name", out _));
    }

    [Fact]
    public async Task Policies_Add_InvalidType_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/policies",
            new { name = "Bad", type = "InvalidType", value = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Policies_Evaluate_ReturnsResult()
    {
        var response = await _client.PostAsJsonAsync("/policies/evaluate",
            new { action = "queue-add", videoUrl = "https://youtube.com/watch?v=test" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("allowed", out _));
    }

    [Fact]
    public async Task Policies_Delete_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync("/policies/nonexistent");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── Worker Auth ───────────────────────────────────────────

    [Fact]
    public async Task WorkerAuth_DevMode_AllowsAllRequests()
    {
        // In dev mode (no Keycloak), the DevelopmentAuthHandler auto-authenticates
        // all requests as admin — even with an invalid X-Worker-Key header
        var request = new HttpRequestMessage(HttpMethod.Post, "/player/play");
        request.Headers.Add("X-Worker-Key", "wrong-key");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Sync Endpoint (MEDIA-725) ─────────────────────────────

    [Fact]
    public async Task Sync_Get_ReturnsSyncSnapshotShape()
    {
        var response = await _client.GetAsync("/sync");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("queue", out var queue));
        Assert.Equal(JsonValueKind.Array, queue.ValueKind);
        Assert.True(body.TryGetProperty("nowPlaying", out _));
        Assert.True(body.TryGetProperty("queueMode", out _));
        Assert.True(body.TryGetProperty("policies", out _));
        Assert.True(body.TryGetProperty("killSwitch", out _));
        Assert.True(body.TryGetProperty("serverTime", out _));
        Assert.True(body.TryGetProperty("version", out _));
    }

    [Fact]
    public async Task Sync_Get_ReturnsETagHeader()
    {
        var response = await _client.GetAsync("/sync");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var etag = response.Headers.ETag;
        Assert.NotNull(etag);
    }

    [Fact]
    public async Task Sync_Get_WithMatchingETag_Returns304()
    {
        // Get initial snapshot to obtain version ETag
        var first = await _client.GetAsync("/sync");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var etag = first.Headers.ETag?.Tag;
        Assert.NotNull(etag);

        // Send request with If-None-Match
        var request = new HttpRequestMessage(HttpMethod.Get, "/sync");
        request.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var second = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
    }

    // ── Player Heartbeat (MEDIA-724) ──────────────────────────

    [Fact]
    public async Task Player_Heartbeat_Returns204()
    {
        var response = await _client.PostAsJsonAsync("/player/heartbeat",
            new { playerId = "living-room", state = "Playing", position = 42.5, videoId = "abc", uptime = 3600, version = "1.0.0" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Players_ReturnsArray()
    {
        var response = await _client.GetAsync("/admin/players");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var arr = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
    }

    // ── Queue Consistency Guard (MEDIA-728) ───────────────────

    [Fact]
    public async Task Queue_Delete_WithStaleVersion_Returns409()
    {
        // Set current version to 5
        _factory.QueueRepository.GetVersionAsync(Arg.Any<CancellationToken>()).Returns(5L);

        var request = new HttpRequestMessage(HttpMethod.Delete, "/queue/some-id");
        request.Headers.Add("X-Queue-Version", "3");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task Queue_Delete_WithMatchingVersion_Succeeds()
    {
        _factory.QueueRepository.GetVersionAsync(Arg.Any<CancellationToken>()).Returns(5L);

        var request = new HttpRequestMessage(HttpMethod.Delete, "/queue/some-id");
        request.Headers.Add("X-Queue-Version", "5");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Queue_Delete_WithoutVersionHeader_Succeeds()
    {
        var response = await _client.DeleteAsync("/queue/some-id");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── Correlation ID (MEDIA-741) ────────────────────────────

    [Fact]
    public async Task Response_ContainsCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(response.Headers.Contains("X-Correlation-Id"),
            "Response must contain X-Correlation-Id header");
        var correlationId = response.Headers.GetValues("X-Correlation-Id").First();
        Assert.False(string.IsNullOrEmpty(correlationId));
    }

    [Fact]
    public async Task CorrelationId_EchoesClientHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-Id", "client-trace-42");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var correlationId = response.Headers.GetValues("X-Correlation-Id").First();
        Assert.Equal("client-trace-42", correlationId);
    }

    // ── Prometheus Metrics (MEDIA-742) ────────────────────────

    [Fact]
    public async Task Metrics_Endpoint_Returns200_WithPrometheusFormat()
    {
        var response = await _client.GetAsync("/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        // UseHttpMetrics() exposes HTTP request duration metrics in Prometheus text format
        Assert.Contains("http_request_duration_seconds", body);
    }

    // ── Worker Registration (MEDIA-729) ───────────────────────

    [Fact]
    public async Task Worker_Register_ReturnsOk_WithRegistrationResponse()
    {
        var response = await _client.PostAsJsonAsync("/worker/register",
            new { name = "Living Room TV", version = "1.0.0", os = "linux" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("playerId", out var playerId));
        Assert.False(string.IsNullOrEmpty(playerId.GetString()));
        Assert.True(body.TryGetProperty("serverTime", out _));
        Assert.True(body.TryGetProperty("config", out var config));
        Assert.True(config.TryGetProperty("heartbeatInterval", out _));
        Assert.True(config.TryGetProperty("positionReportInterval", out _));
        Assert.True(config.TryGetProperty("sseUrl", out _));
    }

    [Fact]
    public async Task Worker_Register_WithCapabilities_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/worker/register",
            new
            {
                name = "Kitchen Pi",
                capabilities = new { cec = true, audioOutput = "hdmi", maxResolution = "1080p" },
                version = "2.0.0",
                os = "linux"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("playerId", out _));
    }

    // ── Player Log Streaming (MEDIA-732) ──────────────────────

    [Fact]
    public async Task Diagnostics_SubmitLogs_Returns204()
    {
        var response = await _client.PostAsJsonAsync("/diagnostics/logs",
            new
            {
                playerId = "living-room-tv",
                entries = new[]
                {
                    new { timestamp = "2026-03-14T10:00:00Z", level = "error", message = "Player error 150", source = "player" },
                    new { timestamp = "2026-03-14T10:00:01Z", level = "info", message = "Reporting error", source = "tv" }
                }
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Diagnostics_SubmitLogs_EmptyEntries_Returns204()
    {
        var response = await _client.PostAsJsonAsync("/diagnostics/logs",
            new { playerId = "test", entries = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Diagnostics_SubmitLogs_MissingPlayerId_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/diagnostics/logs",
            new { playerId = "", entries = new[] { new { timestamp = "t", level = "info", message = "m" } } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_PlayerLogs_ReturnsLogShape()
    {
        var response = await _client.GetAsync("/admin/players/living-room-tv/logs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("playerId", out _));
        Assert.True(body.TryGetProperty("entries", out var entries));
        Assert.Equal(JsonValueKind.Array, entries.ValueKind);
        Assert.True(body.TryGetProperty("totalCount", out _));
    }

    // ── Version Check & Update Notify (MEDIA-733) ─────────────

    [Fact]
    public async Task Admin_VersionMatrix_ReturnsShape()
    {
        var response = await _client.GetAsync("/admin/players/versions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("players", out var players));
        Assert.Equal(JsonValueKind.Array, players.ValueKind);
    }

    [Fact]
    public async Task Admin_SetExpectedVersion_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/admin/players/expected-version",
            new { version = "1.3.0" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("expectedVersion", out var v));
        Assert.Equal("1.3.0", v.GetString());
    }

    [Fact]
    public async Task Admin_NotifyUpdate_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/admin/players/notify-update",
            new { message = "Update available: v1.3.0" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("notified", out var notified));
        Assert.True(notified.GetBoolean());
    }

    // ── Graceful Disconnect (MEDIA-760) ───────────────────────

    [Fact]
    public async Task Worker_Disconnect_ReturnsOk()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/worker/disconnect");
        request.Headers.Add("X-Player-Id", "living-room-tv");
        request.Content = JsonContent.Create(new { reason = "shutdown", signal = "SIGTERM" });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("status", out var status));
        Assert.Equal("offline", status.GetString());
        Assert.True(body.TryGetProperty("playerId", out _));
    }

    [Fact]
    public async Task Worker_Disconnect_MissingPlayerId_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/worker/disconnect",
            new { reason = "shutdown" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── MEDIA-763: Network Connectivity Monitoring ──────────────

    [Fact]
    public async Task Diagnostics_SubmitNetwork_Returns204()
    {
        var response = await _client.PostAsJsonAsync("/diagnostics/network", new
        {
            playerId = "pi-test",
            timestamp = "2026-03-14T10:00:00Z",
            latency = new { avgMs = 12, minMs = 8, maxMs = 45, p95Ms = 28, samples = 4, failures = 0 },
            dns = new { avgResolveMs = 3, failures = 0 },
            bandwidth = new { lastMbps = 85.2, measuredAt = "2026-03-14T09:55:00Z" }
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Diagnostics_SubmitNetwork_MissingPlayerId_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/diagnostics/network", new
        {
            playerId = "",
            timestamp = "2026-03-14T10:00:00Z",
            latency = new { avgMs = 0, minMs = 0, maxMs = 0, p95Ms = 0, samples = 0, failures = 0 },
            dns = new { avgResolveMs = 0, failures = 0 },
            bandwidth = new { lastMbps = 0.0, measuredAt = "2026-03-14T10:00:00Z" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Diagnostics_BandwidthTest_Returns100KBPayload()
    {
        var response = await _client.GetAsync("/diagnostics/bandwidth-test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(102400, content.Length);
    }

    [Fact]
    public async Task Admin_PlayerNetwork_ReturnsShape()
    {
        var response = await _client.GetAsync("/admin/players/pi-test/network");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("trend", out var trend));
        Assert.True(trend.TryGetProperty("latencyTrend", out _));
        Assert.True(trend.TryGetProperty("bandwidthTrend", out _));
    }

    // ── MEDIA-743: Alerting Integration ─────────────────────────

    [Fact]
    public async Task Admin_AlertConfig_ReturnsShape()
    {
        var response = await _client.GetAsync("/admin/alerts/config");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("enabled", out _));
        Assert.True(body.TryGetProperty("cooldownMinutes", out _));
        Assert.True(body.TryGetProperty("channelCount", out _));
    }
}
