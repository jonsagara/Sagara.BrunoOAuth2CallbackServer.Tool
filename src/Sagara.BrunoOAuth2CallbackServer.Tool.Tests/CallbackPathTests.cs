using Sagara.BrunoOAuth2CallbackServer.Tool.Server;

namespace Sagara.BrunoOAuth2CallbackServer.Tool.Tests;

public class CallbackPathTests
{
    [Theory]
    [InlineData(null, "/callback")]
    [InlineData("/callback", "/callback")]
    [InlineData("callback", "/callback")]
    [InlineData("/callback/", "/callback")]
    [InlineData("/oauth/callback", "/oauth/callback")]
    [InlineData("oauth/callback/", "/oauth/callback")]
    [InlineData("/login", "/login")]
    [InlineData("", "/")]
    [InlineData("/", "/")]
    [InlineData("//", "/")]
    public void TryNormalize_ValidPath_ReturnsNormalizedPath(string? path, string expected)
    {
        bool success = CallbackPath.TryNormalize(path, out string? normalizedPath, out string? error);

        Assert.True(success, error);
        Assert.Equal(expected, normalizedPath);
    }

    [Theory]
    [InlineData("/call back")]
    [InlineData(" /callback")]
    [InlineData("/callback?x=1")]
    [InlineData("/callback#frag")]
    [InlineData("/a/../b")]
    public void TryNormalize_InvalidPath_ReturnsError(string path)
    {
        bool success = CallbackPath.TryNormalize(path, out string? normalizedPath, out string? error);

        Assert.False(success);
        Assert.Null(normalizedPath);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("/callback", "/callback", true)]
    [InlineData("/callback/", "/callback", true)]
    [InlineData("/CALLBACK", "/callback", true)]
    [InlineData("/callbackfoo", "/callback", false)]
    [InlineData("/callback/foo", "/callback", false)]
    [InlineData("/", "/callback", false)]
    [InlineData("/", "/", true)]
    [InlineData("", "/", true)]
    [InlineData(null, "/", true)]
    [InlineData("/favicon.ico", "/", false)]
    [InlineData("/oauth/callback", "/oauth/callback", true)]
    public void Matches_ComparesIgnoringCaseAndTrailingSlash(string? requestPath, string callbackPath, bool expected)
    {
        Assert.Equal(expected, CallbackPath.Matches(requestPath: requestPath, callbackPath: callbackPath));
    }
}
