using ModelContextProtocol.Client;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Extensions;
using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Playwright.Tests.Support;

/// <summary>The real server answering at an address, on a loopback port the machine picked.</summary>
/// <remarks>The host is the program's own, <c>McpServerHost.Listening</c>, served by Snail.Sdk's <see cref="InProcessServer"/>, which reads
/// back the port Kestrel bound; what is left here is the deployment's settings the test adds to.</remarks>
internal sealed class ListeningServer : IAsyncDisposable
{
    private static readonly McpEndpoint AnyFreePort = new(McpTransport.Http, new Uri("http://127.0.0.1:0/"), ListensOnEveryInterface: false);

    private readonly InProcessServer _server;

    private ListeningServer(InProcessServer server) => _server = server;

    public IServiceProvider Services => _server.Services;

    public static async Task<ListeningServer> StartAsync(params (string Name, string Value)[] own)
    {
        var settings = Deployment.Settings(own);
        var app = McpServerHost.Listening([], AnyFreePort, ServerConfigExtensions.Read(settings), settings);
        var server = await InProcessServer.ServeAsync(app);

        return new ListeningServer(server);
    }

    /// <summary>Where the server listens, <c>http://127.0.0.1:&lt;port&gt;</c>.</summary>
    public string Address => _server.Address.AbsoluteUri.TrimEnd('/');

    /// <summary>The MCP endpoint under <see cref="Address"/>.</summary>
    public Uri Endpoint => _server.Endpoint;

    /// <summary>A session at <see cref="Endpoint"/>, with <paramref name="query"/> — <c>?tool=…</c> — as its address when given.</summary>
    public Task<McpClient> ConnectAsync(string query = "") =>
        _server.SessionAsync(query.Length == 0 ? null : query.TrimStart('?'));

    public ValueTask DisposeAsync() => _server.DisposeAsync();
}
