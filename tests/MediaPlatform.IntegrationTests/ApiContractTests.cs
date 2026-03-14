using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ApiContractTests(MediaPlatformFactory factory)
    {
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
}
