using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Snail.MCP.Playwright.Tests.Contracts;

/// <summary>The program's readiness is its browser's: <c>/healthz</c> answers 200 only once the browser behind it does.</summary>
/// <remarks>
/// The browser here is a door that answers the knock the way a Playwright browser server answers a plain request — with
/// <c>426 Upgrade Required</c> — which is all the check asks. So the program is started on <see cref="HttpServerProgram"/>'s
/// default readiness, waiting for <c>/healthz</c>, as an orchestrator waits for it.
/// </remarks>
public sealed class HealthContractTests : IAsyncLifetime
{
    private readonly TcpListener _browser = new(IPAddress.Loopback, 0);

    private HttpServerProgram _server = null!;

    public async Task InitializeAsync()
    {
        _browser.Start();
        _ = AnswerEveryKnockAsync();
        int port = ((IPEndPoint)_browser.LocalEndpoint).Port;

        var environment = Deployment.Variables(("WEB__SCRAPING__BROWSER_SERVER", $"ws://127.0.0.1:{port}/"))
            .ToDictionary(variable => variable.Key, variable => (string?)variable.Value);

        _server = await HttpServerProgram.StartAsync("Snail.MCP.Playwright", "SNAIL_PLAYWRIGHT_SERVER", environment);
    }

    public async Task DisposeAsync()
    {
        await _server.DisposeAsync();
        _browser.Stop();
    }

    [Fact]
    public async Task Health_WithABrowserThatAnswers_IsOkWithNoBody()
    {
        using var http = new HttpClient();

        var answer = await http.GetAsync(new Uri(_server.Address, "/healthz"));

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        Assert.Empty(await answer.Content.ReadAsStringAsync());
    }

    private async Task AnswerEveryKnockAsync()
    {
        byte[] refusal = Encoding.ASCII.GetBytes("HTTP/1.1 426 Upgrade Required\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");

        try
        {
            while (true)
            {
                using var knock = await _browser.AcceptTcpClientAsync();
                await knock.GetStream().WriteAsync(refusal);
            }
        }
        catch (Exception stopped) when (stopped is SocketException or ObjectDisposedException)
        {
        }
    }
}
