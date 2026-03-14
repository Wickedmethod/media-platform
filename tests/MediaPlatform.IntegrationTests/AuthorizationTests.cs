using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MediaPlatform.IntegrationTests;

/// <summary>
/// Tests verifying the dual access model (MEDIA-713):
/// - TV (Worker Key) can access read + queue-add endpoints
/// - TV cannot access admin, policies, or player control endpoints
/// - Unauthenticated requests are rejected
/// </summary>
public class AuthorizationTests : IClassFixture<AuthTestFactory>
{
    private readonly HttpClient _client;

    public AuthorizationTests(AuthTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    private HttpRequestMessage WithWorkerKey(HttpRequestMessage request)
    {
        request.Headers.Add("X-Worker-Key", AuthTestFactory.WorkerKey);
        return request;
    }

    // ── TV (Worker Key) — Allowed Endpoints ────────────────

    [Fact]
    public async Task WorkerKey_CanGetQueue()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Get, "/queue"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WorkerKey_CanGetNowPlaying()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Get, "/now-playing"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WorkerKey_CanAddToQueue()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Post, "/queue/add")
        {
            Content = JsonContent.Create(new { url = "https://youtube.com/watch?v=dQw4w9WgXcQ", title = "Test" })
        });
        var response = await _client.SendAsync(request);
        // 201 Created (successful add)
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task WorkerKey_QueueAdd_SetsGuestIdentity()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Post, "/queue/add")
        {
            Content = JsonContent.Create(new { url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ", title = "Guest Test" })
        });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("tv-guest", body.GetProperty("addedByUserId").GetString());
        Assert.Equal("TV", body.GetProperty("addedByName").GetString());
    }

    [Fact]
    public async Task WorkerKey_CanGetSync()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Get, "/sync"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WorkerKey_CanGetQueueMode()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Get, "/queue/mode"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WorkerKey_CanRegisterWorker()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Post, "/worker/register")
        {
            Content = JsonContent.Create(new { name = "test-tv", version = "1.0.0", os = "linux" })
        });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WorkerKey_CanReportHeartbeat()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Post, "/player/heartbeat")
        {
            Content = JsonContent.Create(new { playerId = "test-tv", state = "Playing", position = 42.5 })
        });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── TV (Worker Key) — Denied Endpoints ─────────────────

    [Fact]
    public async Task WorkerKey_CannotDeleteQueueItem()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Delete, "/queue/some-id"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WorkerKey_CannotAccessAdmin()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Get, "/admin/players"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WorkerKey_CannotAccessPolicies()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Get, "/policies"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WorkerKey_CannotPlayPauseSkipStop()
    {
        var actions = new[] { "/player/play", "/player/pause", "/player/skip", "/player/stop" };
        foreach (var action in actions)
        {
            var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Post, action));
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task WorkerKey_CannotSetQueueMode()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Post, "/queue/mode")
        {
            Content = JsonContent.Create(new { mode = "Shuffle" })
        });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WorkerKey_CannotAccessAnalytics()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Get, "/analytics"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WorkerKey_CannotAccessWebhooks()
    {
        var request = WithWorkerKey(new HttpRequestMessage(HttpMethod.Get, "/webhooks"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Unauthenticated — All Rejected ─────────────────────

    [Fact]
    public async Task NoAuth_QueueReturns401()
    {
        var response = await _client.GetAsync("/queue");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NoAuth_NowPlayingReturns401()
    {
        var response = await _client.GetAsync("/now-playing");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NoAuth_AdminReturns401()
    {
        var response = await _client.GetAsync("/admin/players");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Invalid Worker Key ─────────────────────────────────

    [Fact]
    public async Task InvalidWorkerKey_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/queue");
        request.Headers.Add("X-Worker-Key", "wrong-key");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── SSE Worker Key via Query Param ─────────────────────

    [Fact]
    public async Task WorkerKey_ViaQueryParam_CanAccessEvents()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/events?worker-key={AuthTestFactory.WorkerKey}");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            var response = await _client.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, cts.Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        }
        catch (OperationCanceledException)
        {
            // Expected — SSE is a long-lived connection, we just check the headers
        }
    }

    // ── Health endpoints remain public ─────────────────────

    [Fact]
    public async Task Health_DoesNotRequireAuth()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthLive_DoesNotRequireAuth()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
