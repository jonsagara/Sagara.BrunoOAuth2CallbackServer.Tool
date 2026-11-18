using System.CommandLine;
using Sagara.BrunoOAuth2CallbackServer.Tool;
using Sagara.BrunoOAuth2CallbackServer.Tool.Server;

RootCommand rootCommand = CommandLine.CreateRootCommand(RunAsync);

return await rootCommand.Parse(args).InvokeAsync();


//
// Private methods
//

/// <summary>
/// Starts the server and runs until Ctrl+C.
/// </summary>
static async Task<int> RunAsync(int? port, string callbackPath, CancellationToken cancellationToken)
{
    CallbackServer server;

    try
    {
        server = await CallbackServer.StartAsync(
            candidatePorts: CallbackServer.GetCandidatePorts(port),
            callbackPath: callbackPath,
            cancellationToken: cancellationToken);
    }
    catch (PortUnavailableException) when (port is not null)
    {
        return Fail($"Port {port} is already in use. Please try a different port.");
    }
    catch (PortUnavailableException)
    {
        return Fail($"No port is available between {CallbackServer.DefaultPort} and {CallbackServer.LastScannedPort}. Use --port to choose one.");
    }
    catch (OperationCanceledException)
    {
        return 0;
    }

    await using (server)
    {
        if (port is null && server.Port != CallbackServer.DefaultPort)
        {
            Console.Error.WriteLine($"warning: Port {CallbackServer.DefaultPort} is in use, so using port {server.Port}. The callback URL in Bruno and in your OAuth2 provider must use this port.");
        }

        Console.WriteLine($"OAuth2 callback server running at {server.CallbackUrl}");

        // System.CommandLine cancels the token when the user presses Ctrl+C.
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    return 0;
}

static int Fail(string message)
{
    Console.Error.WriteLine($"error: {message}");
    return 1;
}
