using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Sagara.BrunoOAuth2CallbackServer.Tool.Server;

/// <summary>
/// A Kestrel server, bound to 127.0.0.1, that serves <see cref="CallbackPage"/> at the callback path.
/// </summary>
internal sealed class CallbackServer : IAsyncDisposable
{
    public const int DefaultPort = 8090;

    /// <summary>
    /// When no port is specified, try this many ports, starting at <see cref="DefaultPort"/>.
    /// </summary>
    public const int PortScanCount = 100;

    public const int LastScannedPort = DefaultPort + PortScanCount - 1;

    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    private readonly WebApplication _app;

    private CallbackServer(WebApplication app, int port, string callbackPath)
    {
        _app = app;
        Port = port;
        CallbackPath = callbackPath;
    }

    /// <summary>
    /// The port the server is listening on.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// The normalized path the callback page is served from.
    /// </summary>
    public string CallbackPath { get; }

    public string CallbackUrl => $"http://127.0.0.1:{Port}{CallbackPath}";

    /// <summary>
    /// The ports to try, in order: just the requested port if there is one; otherwise, an upward scan
    /// from <see cref="DefaultPort"/>.
    /// </summary>
    public static IReadOnlyList<int> GetCandidatePorts(int? requestedPort)
        => requestedPort is int port
            ? [port]
            : [.. Enumerable.Range(DefaultPort, PortScanCount)];

    /// <summary>
    /// Starts the server on the first candidate port that can be bound. Port 0 binds to a port chosen by
    /// the OS.
    /// </summary>
    /// <exception cref="PortUnavailableException">None of the candidate ports could be bound.</exception>
    public static async Task<CallbackServer> StartAsync(IReadOnlyList<int> candidatePorts, string callbackPath, CancellationToken cancellationToken)
    {
        foreach (int port in candidatePorts)
        {
            WebApplication app = Build(port: port, callbackPath: callbackPath);

            try
            {
                await app.StartAsync(cancellationToken);

                // Read back the bound port, which differs from the requested one when port is 0.
                int boundPort = new Uri(app.Urls.Single()).Port;

                return new CallbackServer(app: app, port: boundPort, callbackPath: callbackPath);
            }
            catch (Exception ex) when (IsPortUnavailable(ex))
            {
                await app.DisposeAsync();
            }
            catch
            {
                await app.DisposeAsync();
                throw;
            }
        }

        throw new PortUnavailableException(candidatePorts);
    }

    public async ValueTask DisposeAsync()
    {
        using CancellationTokenSource timeoutCts = new(ShutdownTimeout);

        try
        {
            await _app.StopAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Took too long to drain connections. Disposing will close them.
        }

        await _app.DisposeAsync();
    }


    //
    // Private methods
    //

    private static WebApplication Build(int port, string callbackPath)
    {
        // The empty builder reads no configuration (no appsettings.json from the current directory, no
        //   ASPNETCORE_* environment variables) and adds no logging providers. That keeps request logging,
        //   which would include the authorization code in the query string, out of the console.
        WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.WebHost
            .UseKestrelCore()
            .ConfigureKestrel(options =>
            {
                options.AddServerHeader = false;
                options.Listen(IPAddress.Loopback, port);
            });

        WebApplication app = builder.Build();

        app.Run(context => HandleRequestAsync(context, callbackPath));

        return app;
    }

    private static async Task HandleRequestAsync(HttpContext context, string callbackPath)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        if (!Tool.Server.CallbackPath.Matches(requestPath: request.Path.Value, callbackPath: callbackPath))
        {
            response.StatusCode = StatusCodes.Status404NotFound;
            response.ContentType = "text/plain; charset=utf-8";
            await response.WriteAsync("Not Found", context.RequestAborted);
            return;
        }

        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            response.Headers.Allow = "GET, HEAD";
            return;
        }

        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "text/html; charset=utf-8";

        // The query string carries the authorization code: don't cache the page, and don't leak the URL
        //   in a Referer header.
        response.Headers.CacheControl = "no-store";
        response.Headers["Referrer-Policy"] = "no-referrer";

        if (HttpMethods.IsHead(request.Method))
        {
            return;
        }

        await response.WriteAsync(CallbackPage.Html, context.RequestAborted);
    }

    /// <summary>
    /// True if binding failed because the port is in use, or, on Windows, because the port is in an
    /// excluded port range (e.g., reserved by Hyper-V), which surfaces as access denied.
    /// </summary>
    private static bool IsPortUnavailable(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            if (current is AddressInUseException)
            {
                return true;
            }

            if (current is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse or SocketError.AccessDenied })
            {
                return true;
            }
        }

        return false;
    }
}
