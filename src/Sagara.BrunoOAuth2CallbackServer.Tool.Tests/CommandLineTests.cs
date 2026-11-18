using System.CommandLine;

namespace Sagara.BrunoOAuth2CallbackServer.Tool.Tests;

public class CommandLineTests
{
    [Theory]
    [InlineData(new string[0], null, "/callback")]
    [InlineData(new[] { "--port", "8090" }, 8090, "/callback")]
    [InlineData(new[] { "-p", "9000" }, 9000, "/callback")]
    [InlineData(new[] { "-p", "1" }, 1, "/callback")]
    [InlineData(new[] { "-p", "65535" }, 65535, "/callback")]
    [InlineData(new[] { "--path", "/oauth/callback/" }, null, "/oauth/callback")]
    [InlineData(new[] { "--path", "login" }, null, "/login")]
    [InlineData(new[] { "--path", "" }, null, "/")]
    [InlineData(new[] { "--path", "/" }, null, "/")]
    [InlineData(new[] { "-p", "3000", "--path", "/oauth/callback" }, 3000, "/oauth/callback")]
    public async Task ValidArguments_PassPortAndNormalizedPathToAction(string[] args, int? expectedPort, string expectedPath)
    {
        int? actualPort = -1;
        string? actualPath = null;

        RootCommand rootCommand = CommandLine.CreateRootCommand((port, path, _) =>
        {
            actualPort = port;
            actualPath = path;
            return Task.FromResult(0);
        });

        int exitCode = await rootCommand.Parse(args).InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedPort, actualPort);
        Assert.Equal(expectedPath, actualPath);
    }

    [Theory]
    [InlineData("-p", "0")]
    [InlineData("-p", "65536")]
    [InlineData("-p", "-1")]
    [InlineData("-p", "abc")]
    [InlineData("--path", "/a?b")]
    [InlineData("--path", "/a#b")]
    [InlineData("--path", "/a b")]
    [InlineData("--bogus")]
    public void InvalidArguments_ProduceParseErrors(params string[] args)
    {
        RootCommand rootCommand = CommandLine.CreateRootCommand((_, _, _) => Task.FromResult(0));

        ParseResult parseResult = rootCommand.Parse(args);

        Assert.NotEmpty(parseResult.Errors);
    }
}
