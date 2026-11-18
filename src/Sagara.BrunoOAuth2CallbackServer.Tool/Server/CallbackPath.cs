using System.Diagnostics.CodeAnalysis;

namespace Sagara.BrunoOAuth2CallbackServer.Tool.Server;

/// <summary>
/// Validates and normalizes the path that the callback page is served from.
/// </summary>
internal static class CallbackPath
{
    public const string Default = "/callback";

    /// <summary>
    /// Normalizes a user-supplied callback path: adds a leading slash and removes trailing slashes. An empty
    /// string or "/" means the root path. Returns false with an error message if the path can't be used.
    /// </summary>
    public static bool TryNormalize(string? path, [NotNullWhen(true)] out string? normalizedPath, [NotNullWhen(false)] out string? error)
    {
        normalizedPath = null;
        error = null;

        path ??= Default;

        if (path.Any(char.IsWhiteSpace))
        {
            error = "The callback path cannot contain whitespace.";
            return false;
        }

        if (path.Contains('?') || path.Contains('#'))
        {
            error = "The callback path cannot contain a query string (?) or fragment (#).";
            return false;
        }

        string candidate = "/" + path.Trim('/');

        if (!Uri.TryCreate("http://127.0.0.1" + candidate, UriKind.Absolute, out Uri? uri) || uri.AbsolutePath != candidate)
        {
            error = $"'{path}' is not a valid URL path.";
            return false;
        }

        normalizedPath = candidate;
        return true;
    }

    /// <summary>
    /// Returns true if the request path matches the callback path. The comparison ignores case and a
    /// trailing slash.
    /// </summary>
    public static bool Matches(string? requestPath, string callbackPath)
    {
        string trimmed = "/" + (requestPath ?? string.Empty).Trim('/');
        return string.Equals(trimmed, callbackPath, StringComparison.OrdinalIgnoreCase);
    }
}
