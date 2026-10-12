using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Playwright.Extensions;

/// <summary>The server as a running host: listening at an address when it was given a port, a child process otherwise.</summary>
public static class McpServerHost
{
    /// <summary>The path a session is opened at, and the one the platform writes into its registry.</summary>
    public const string Path = "/mcp";

    /// <summary>The path that says whether the server and its browser are alive.</summary>
    public const string HealthPath = "/healthz";

    /// <summary>Builds whichever of the two shapes the configuration asks for.</summary>
    public static IHost Of(string[] args)
    {
        var settings = ServerConfigExtensions.Settings();
        var config = ServerConfigExtensions.Read(settings);

        return config.Port > 0 ? Listening(args, config, settings) : Spoken(args, config, settings);
    }

    /// <summary>The server answering sessions at <see cref="ServerConfig.Port"/>; a port of zero takes any free one.</summary>
    /// <remarks>
    /// Bound on every interface because the callers are the workers of the machine, each in its own
    /// container: a server on loopback would be reachable from nothing but itself. There is no token for
    /// the same reason there is no published port — the platform hands a server at an address no secret,
    /// so the network the container stands in is the whole of its access control.
    /// </remarks>
    internal static WebApplication Listening(string[] args, ServerConfig config, IConfiguration settings)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.ConfigureMcpServerAtAnAddress(config, settings);

        var app = builder.Build();

        app.Urls.Add($"http://*:{config.Port}");
        app.MapGet(HealthPath, AliveAsync);
        app.MapMcp(Path);

        return app;
    }

    private static IHost Spoken(string[] args, ServerConfig config, IConfiguration settings)
    {
        var builder = McpHost.CreateBuilder(args);

        builder.ConfigureMcpServerAsAProgram(config, settings);

        return builder.Build();
    }

    private static async Task<IResult> AliveAsync(Health health, CancellationToken cancellationToken) =>
        await health.IsBrowserReachableAsync(cancellationToken)
            ? Results.Ok()
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
}
