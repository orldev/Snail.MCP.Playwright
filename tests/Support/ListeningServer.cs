using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Extensions;
using Snail.Sdk.Mcp.Hosting;
using Snail.Sdk.Mcp.Http;

namespace Snail.MCP.Playwright.Tests.Support;

/// <summary>The real server answering at an address, on a loopback port the machine picked.</summary>
internal sealed class ListeningServer : IAsyncDisposable
{
    private static readonly McpEndpoint AnyFreePort = new(McpTransport.Http, new Uri("http://127.0.0.1:0/"), ListensOnEveryInterface: false);

    private readonly WebApplication _app;

    private ListeningServer(WebApplication app) => _app = app;

    public IServiceProvider Services => _app.Services;

    public static async Task<ListeningServer> StartAsync(params (string Name, string Value)[] own)
    {
        var settings = Deployment.Settings(own);
        var app = McpServerHost.Listening([], AnyFreePort, ServerConfigExtensions.Read(settings), settings);

        await app.StartAsync();

        return new ListeningServer(app);
    }

    /// <summary>Where the server listens, <c>http://127.0.0.1:&lt;port&gt;</c>.</summary>
    public string Address =>
        _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First().TrimEnd('/');

    /// <summary>The MCP endpoint under <see cref="Address"/>.</summary>
    public Uri Endpoint => new($"{Address}{McpHttpHost.DefaultPath}");

    public Task<McpClient> ConnectAsync(string query = "") =>
        McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"{Endpoint}{query}")
        }));

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
