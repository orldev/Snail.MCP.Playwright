using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.Sdk.Mcp.Hosting;
using Snail.Sdk.Mcp.Http;

namespace Snail.MCP.Playwright.Extensions;

/// <summary>The server as a running host: listening at an address when it was given a port, a child process otherwise.</summary>
public static class McpServerHost
{
    /// <summary>Where HTTP listens when the settings name a transport but no address: the port the image publishes, on loopback.</summary>
    /// <remarks>
    /// Inside a container every interface is listened on whatever the host says, since a container's published port does
    /// not reach its loopback and the published port is the boundary; outside one a developer's run stays on loopback.
    /// </remarks>
    public static Uri DefaultUrl { get; } = new("http://127.0.0.1:8822/");

    /// <summary>Builds whichever of the two shapes the configuration asks for.</summary>
    public static IHost Of(string[] args)
    {
        var settings = ServerConfigExtensions.Settings();
        var config = ServerConfigExtensions.Read(settings);
        var endpoint = EndpointOf(settings);

        return endpoint.Transport is McpTransport.Http ? Listening(args, endpoint, config, settings) : Spoken(args, endpoint, config, settings);
    }

    /// <summary>How the endpoint is read: a key at the root first, then under <c>Snail</c>, where the server's own settings may be nested; loopback outside a container, every interface inside one.</summary>
    /// <remarks>
    /// There is no token, so the reach is the whole of the access control: outside a container a URL beyond loopback or
    /// <c>ListensOnEveryInterface</c> is refused at the start, naming the variable, rather than served to the network.
    /// The server's name makes a refusal spell <c>SNAIL_MCP_PLAYWRIGHT_…</c> wherever it is read, the test runner included.
    /// Inside the image every interface is listened on, since a container's published port does not reach its loopback.
    /// </remarks>
    public static McpEndpointOptions EndpointOptions { get; } = new()
    {
        FallbackSection = ServerConfig.SectionName,
        Reach = McpReach.LoopbackOrContainer,
        Server = ServerConfigExtensions.Name
    };

    /// <summary>Where the server answers: <c>PORT</c> alone means HTTP on that port, <c>TRANSPORT</c> with <c>URL</c> says it outright, neither means stdio.</summary>
    /// <exception cref="StartRefusedException">A key holds what it cannot mean, or an address is beyond the server's reach, named by its variable.</exception>
    public static McpEndpoint EndpointOf(IConfiguration settings) => McpEndpoint.Read(settings, DefaultUrl, EndpointOptions);

    /// <summary>The server answering calls at the endpoint's address.</summary>
    /// <remarks>
    /// There is no token: the platform hands a server at an address no secret, so the network the container stands in is
    /// the whole of its access control. The host — loopback, or every interface inside a container — the refusal of
    /// every request a browser makes, log lines on stderr and the heap trimmed when the server goes quiet are
    /// Snail.Sdk.Mcp.Http's.
    /// <para>
    /// <c>/healthz</c> asks whether the browser behind the server answers, not only whether the process runs: an
    /// orchestrator restarts this one by whether it can read a page, so the check answers 503 while the browser does not.
    /// </para>
    /// </remarks>
    internal static WebApplication Listening(string[] args, McpEndpoint endpoint, ServerConfig config, IConfiguration settings)
    {
        var builder = McpHttpHost.CreateBuilder(args, endpoint, settings);

        builder.ConfigureMcpServerAtAnAddress(endpoint, config, settings);

        var app = builder.Build();

        app.MapLocalMcp();
        app.MapHealth(McpHttpHost.DefaultHealthPath, IsBrowserReachableAsync);

        return app;
    }

    private static IHost Spoken(string[] args, McpEndpoint endpoint, ServerConfig config, IConfiguration settings)
    {
        var builder = McpHost.CreateBuilder(args, settings);

        builder.ConfigureMcpServerAsAProgram(endpoint, config, settings);

        return builder.Build();
    }

    private static async Task<HealthAnswer> IsBrowserReachableAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        bool isReachable = await services.GetRequiredService<Health>().IsBrowserReachableAsync(cancellationToken);

        return new HealthAnswer(isReachable);
    }
}
