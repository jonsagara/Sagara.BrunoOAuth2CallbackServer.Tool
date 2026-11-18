namespace Sagara.BrunoOAuth2CallbackServer.Tool.Server;

/// <summary>
/// The page served at the callback path. It forwards the OAuth2 response to Bruno through the bruno://
/// URL scheme. This runs in the browser because the URL fragment (used by the implicit flow) is never
/// sent to the server.
/// </summary>
/// <remarks>
/// <para>
/// Upstream calls window.open(url), which Safari 26 treats as a pop-up and blocks
/// (https://github.com/usebruno/oauth2-callback-server/issues/7). Navigating with window.location.href
/// works in all browsers.
/// </para>
/// Ported from https://github.com/usebruno/oauth2-callback-server (MIT). See THIRD-PARTY-NOTICES.md.
/// </remarks>
internal static class CallbackPage
{
    public const string BrunoCallbackUrl = "bruno://app/oauth2/callback";

    public const string Html = $$"""
        <!doctype html>
        <html>
          <head>
            <title>Bruno OAuth2 Redirect</title>
            <script>
              (function() {
                var url = '{{BrunoCallbackUrl}}';
                url += window.location.search || '';
                url += window.location.hash || '';
                window.location.href = url;
              })();
            </script>
          </head>
          <body>
            Redirecting to Bruno...
          </body>
        </html>
        """;
}
