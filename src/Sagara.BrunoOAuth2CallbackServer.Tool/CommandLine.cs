using System.CommandLine;
using System.Globalization;
using Sagara.BrunoOAuth2CallbackServer.Tool.Server;

namespace Sagara.BrunoOAuth2CallbackServer.Tool;

/// <summary>
/// Defines the command line options. Kept separate from Program.cs so the tests can parse arguments.
/// </summary>
internal static class CommandLine
{
    public const int MinPort = 1;
    public const int MaxPort = 65535;

    public static readonly Option<int?> PortOption = CreatePortOption();
    public static readonly Option<string> PathOption = CreatePathOption();

    /// <summary>
    /// Creates the root command. <paramref name="run"/> receives the port (null if not specified), the
    /// normalized callback path, and a token that is canceled on Ctrl+C.
    /// </summary>
    public static RootCommand CreateRootCommand(Func<int?, string, CancellationToken, Task<int>> run)
    {
        // Each option is added with C#'s goofy inferred-.Add() syntax.
        RootCommand rootCommand = new("OAuth2 callback server for Bruno. Serves a page that forwards the OAuth2 redirect from your browser to Bruno.")
        {
            PortOption,
            PathOption,
        };

        rootCommand.SetAction(
            (parseResult, cancellationToken) =>
                run(
                    parseResult.GetValue(PortOption),
                    NormalizePath(parseResult.GetValue(PathOption)),
                    cancellationToken));

        return rootCommand;
    }


    //
    // Private methods
    //

    private static Option<int?> CreatePortOption()
    {
        Option<int?> option = new("--port", "-p")
        {
            Description = $"Port number (default: {CallbackServer.DefaultPort}, or the next available port up to {CallbackServer.LastScannedPort}).",

            // A custom parser rather than a validator, so non-numeric input gets the same error as an
            //   out-of-range number instead of a conversion exception.
            CustomParser = result =>
            {
                string token = result.Tokens.Single().Value;

                if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int port) || port is < MinPort or > MaxPort)
                {
                    result.AddError($"Invalid port number '{token}'. Port must be between {MinPort} and {MaxPort}.");
                    return null;
                }

                return port;
            },
        };

        return option;
    }

    private static Option<string> CreatePathOption()
    {
        Option<string> option = new("--path")
        {
            Description = $"The path to serve the callback page from (default: {Server.CallbackPath.Default}). Use \"\" or / for the root path.",
            DefaultValueFactory = _ => Server.CallbackPath.Default,
        };

        option.Validators.Add(result =>
        {
            if (!Server.CallbackPath.TryNormalize(result.GetValueOrDefault<string>(), out _, out string? error))
            {
                result.AddError(error);
            }
        });

        return option;
    }

    private static string NormalizePath(string? path)
        => Server.CallbackPath.TryNormalize(path, out string? normalizedPath, out string? error)
            ? normalizedPath
            : throw new InvalidOperationException(error);
}
