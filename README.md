# dotnet-bruno-oauth2-callback-server (officially Sagara.BrunoOAuth2CallbackServer.Tool)

A .NET global tool that runs a local OAuth2 callback server for the [Bruno](https://www.usebruno.com/) API client. When you authorize in your system browser, the OAuth2 provider redirects to this server, which serves a page that forwards the response (query string and fragment) to Bruno through the `bruno://` URL scheme.

It is a .NET port of [@usebruno/oauth2-callback-server](https://github.com/usebruno/oauth2-callback-server), with an added `--path` option and a fix for the redirect being blocked in Safari. It is not affiliated with Bruno.

Requires the .NET 10 or .NET 11 runtime (or later).

## Install

```
dotnet tool install -g Sagara.BrunoOAuth2CallbackServer.Tool
```

Or run it without installing:

```
dnx Sagara.BrunoOAuth2CallbackServer.Tool
```

## Usage

```
> dotnet-bruno-oauth2-callback-server
OAuth2 callback server running at http://127.0.0.1:8090/callback
```

Because the command starts with `dotnet-`, you can also run it as `dotnet bruno-oauth2-callback-server`.

Then, in Bruno:

1. In the collection or request's OAuth2 settings, enable the option to authorize in the system browser.
2. Set the callback URL to `http://localhost:8090/callback` (or the URL the server printed).
3. Register the same callback URL with your OAuth2 provider.

Press Ctrl+C to stop the server.

### Options

| Option | Description |
|---|---|
| `-p, --port <port>` | The port to listen on. If the port is in use, the tool exits with an error. Without this option, the tool tries 8090, then 8091, and so on up to 8189, and prints a warning if it doesn't get 8090. |
| `--path <path>` | The path to serve the callback page from. Defaults to `/callback`. Use `""` or `/` for the root path. |
| `-h, --help` | Show help. |
| `--version` | Show the version. |

For example, to match a provider configured for `http://localhost:3000/oauth/callback`:

```
dotnet-bruno-oauth2-callback-server --port 3000 --path /oauth/callback
```

## Details

- The server listens on `127.0.0.1` only.
- The callback page is served for `GET` and `HEAD`. The path match ignores case and a trailing slash. Other methods get `405`, and other paths get `404`.
- The page is sent with `Cache-Control: no-store` and `Referrer-Policy: no-referrer`, because its URL contains the authorization code. Requests are not logged.
- The tool reads no configuration files or `ASPNETCORE_*` environment variables.

## Building

```
dotnet build
dotnet test
dotnet pack src/Sagara.BrunoOAuth2CallbackServer.Tool -o artifacts
dotnet tool install -g Sagara.BrunoOAuth2CallbackServer.Tool --add-source ./artifacts --prerelease
```

Building requires the .NET 11 SDK (see `global.json`). Running the tests requires both the .NET 10 and .NET 11 runtimes.
