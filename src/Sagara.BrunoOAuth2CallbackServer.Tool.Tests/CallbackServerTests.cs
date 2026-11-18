using System.Net;
using System.Net.Sockets;
using Sagara.BrunoOAuth2CallbackServer.Tool.Server;

namespace Sagara.BrunoOAuth2CallbackServer.Tool.Tests;

public class CallbackServerTests
{
    private static readonly HttpClient HttpClient = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public void GetCandidatePorts_NoPort_ScansUpwardFromDefault()
    {
        IReadOnlyList<int> ports = CallbackServer.GetCandidatePorts(requestedPort: null);

        Assert.Equal(CallbackServer.PortScanCount, ports.Count);
        Assert.Equal(CallbackServer.DefaultPort, ports[0]);
        Assert.Equal(CallbackServer.DefaultPort + 1, ports[1]);
        Assert.Equal(CallbackServer.LastScannedPort, ports[^1]);
    }

    [Fact]
    public void GetCandidatePorts_RequestedPort_ReturnsOnlyThatPort()
    {
        Assert.Equal([9000], CallbackServer.GetCandidatePorts(requestedPort: 9000));
    }

    [Fact]
    public async Task Get_CallbackPath_ReturnsRedirectPage()
    {
        await using CallbackServer server = await StartAsync();

        using HttpResponseMessage response = await HttpClient.GetAsync($"{server.CallbackUrl}?code=abc&state=xyz", CancellationToken);
        string body = await response.Content.ReadAsStringAsync(CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Empty(response.Headers.Server);
        Assert.Contains("'bruno://app/oauth2/callback'", body);
        Assert.Contains("window.location.search", body);
        Assert.Contains("window.location.hash", body);

        // Safari 26 blocks window.open as a pop-up; see CallbackPage.
        Assert.Contains("window.location.href = url", body);
        Assert.DoesNotContain("window.open", body);
    }

    [Fact]
    public async Task Head_CallbackPath_ReturnsOkWithoutBody()
    {
        await using CallbackServer server = await StartAsync();

        using HttpRequestMessage request = new(HttpMethod.Head, server.CallbackUrl);
        using HttpResponseMessage response = await HttpClient.SendAsync(request, CancellationToken);
        byte[] body = await response.Content.ReadAsByteArrayAsync(CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(body);
    }

    [Fact]
    public async Task Post_CallbackPath_ReturnsMethodNotAllowed()
    {
        await using CallbackServer server = await StartAsync();

        using HttpResponseMessage response = await HttpClient.PostAsync(server.CallbackUrl, content: null, CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(["GET", "HEAD"], response.Content.Headers.Allow.Order());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/callbackfoo")]
    [InlineData("/callback/foo")]
    [InlineData("/favicon.ico")]
    public async Task Get_OtherPath_ReturnsNotFound(string path)
    {
        await using CallbackServer server = await StartAsync();

        using HttpResponseMessage response = await HttpClient.GetAsync($"http://127.0.0.1:{server.Port}{path}", CancellationToken);
        string body = await response.Content.ReadAsStringAsync(CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Not Found", body);
    }

    [Theory]
    [InlineData("/oauth/callback", "/oauth/callback")]
    [InlineData("/oauth/callback", "/OAuth/Callback/")]
    [InlineData("/", "/")]
    [InlineData("/", "")]
    public async Task Get_CustomPath_ReturnsRedirectPage(string callbackPath, string requestPath)
    {
        await using CallbackServer server = await StartAsync(callbackPath);

        using HttpResponseMessage response = await HttpClient.GetAsync($"http://127.0.0.1:{server.Port}{requestPath}", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_DefaultPathWhenCustomPathConfigured_ReturnsNotFound()
    {
        await using CallbackServer server = await StartAsync("/oauth/callback");

        using HttpResponseMessage response = await HttpClient.GetAsync($"http://127.0.0.1:{server.Port}/callback", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StartAsync_FirstPortInUse_UsesNextPort()
    {
        using TcpListener blocker = StartBlocker(out int blockedPort);

        await using CallbackServer server = await CallbackServer.StartAsync(candidatePorts: [blockedPort, 0], callbackPath: CallbackPath.Default, cancellationToken: CancellationToken);

        Assert.NotEqual(blockedPort, server.Port);
        Assert.NotEqual(0, server.Port);
    }

    [Fact]
    public async Task StartAsync_AllPortsInUse_ThrowsPortUnavailableException()
    {
        using TcpListener blocker = StartBlocker(out int blockedPort);

        PortUnavailableException ex = await Assert.ThrowsAsync<PortUnavailableException>(
            () => CallbackServer.StartAsync(candidatePorts: [blockedPort], callbackPath: CallbackPath.Default, cancellationToken: CancellationToken));

        Assert.Equal([blockedPort], ex.CandidatePorts);
    }

    [Fact]
    public async Task DisposeAsync_ReleasesPort()
    {
        CallbackServer server = await StartAsync();
        int port = server.Port;
        await server.DisposeAsync();

        await using CallbackServer secondServer = await CallbackServer.StartAsync(candidatePorts: [port], callbackPath: CallbackPath.Default, cancellationToken: CancellationToken);

        Assert.Equal(port, secondServer.Port);
    }

    [Fact]
    public async Task CallbackUrl_UsesLoopbackPortAndPath()
    {
        await using CallbackServer server = await StartAsync("/oauth/callback");

        Assert.Equal($"http://127.0.0.1:{server.Port}/oauth/callback", server.CallbackUrl);
    }


    //
    // Private methods
    //

    /// <summary>
    /// Starts a server on a port chosen by the OS, so tests can run in parallel.
    /// </summary>
    private static Task<CallbackServer> StartAsync(string callbackPath = CallbackPath.Default)
        => CallbackServer.StartAsync(candidatePorts: [0], callbackPath: callbackPath, cancellationToken: CancellationToken);

    private static TcpListener StartBlocker(out int port)
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return listener;
    }
}
