using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Snail.MCP.Playwright.Tests.Chaos;

/// <summary>
/// Breaks the browser behind the server the way a restarting container or a dead proxy does: the port accepts the
/// connection and nothing ever answers on it. A connection refused is what every other test uses, and it is the
/// easy case; a connection that hangs is the one that holds a worker.
/// </summary>
public sealed class HealthChaosTests
{
    [Fact]
    public async Task Diagnose_WithABrowserServerThatAcceptsAndNeverAnswers_ReportsWithinTheProbeTime()
    {
        using var hanging = Hanging(out int port);

        await using var server = await ListeningServer.StartAsync(("WEB__SCRAPING__BROWSER_SERVER", $"ws://127.0.0.1:{port}/"), ("BROWSER_PROBE_SECONDS", "3"));
        await using var client = await server.ConnectAsync();
        long asked = Stopwatch.GetTimestamp();

        var answer = await client.CallToolAsync("playwright_diagnose", new Dictionary<string, object?>()).AsTask().WaitAsync(TimeSpan.FromSeconds(60));
        double seconds = Stopwatch.GetElapsedTime(asked).TotalSeconds;

        Assert.False(answer.Data()!["browser"]!["isReachable"]!.GetValue<bool>());
        Assert.InRange(seconds, 0, 15);
    }

    /// <summary>The knock takes no browser, but a process that accepts and never reacts is still nobody, and the orchestrator is told so in time.</summary>
    [Fact]
    public async Task Health_WithABrowserServerThatAcceptsAndNeverAnswers_IsUnavailableWithinTheProbeTime()
    {
        using var hanging = Hanging(out int port);

        await using var server = await ListeningServer.StartAsync(("WEB__SCRAPING__BROWSER_SERVER", $"ws://127.0.0.1:{port}/"), ("BROWSER_PROBE_SECONDS", "3"));
        using var http = new HttpClient();
        long asked = Stopwatch.GetTimestamp();

        var answer = await http.GetAsync(new Uri($"{server.Address}/healthz")).WaitAsync(TimeSpan.FromSeconds(60));
        double seconds = Stopwatch.GetElapsedTime(asked).TotalSeconds;

        Assert.Equal(HttpStatusCode.ServiceUnavailable, answer.StatusCode);
        Assert.InRange(seconds, 0, 15);
    }

    /// <summary>A listener that accepts every connection and keeps it, answering nothing: a container being restarted, a dead proxy.</summary>
    private static TcpListener Hanging(out int port)
    {
        var hanging = new TcpListener(IPAddress.Loopback, 0);
        hanging.Start();
        var held = new List<TcpClient>();
        _ = Task.Run(async () =>
        {
            while (true)
                held.Add(await hanging.AcceptTcpClientAsync());
        });
        port = ((IPEndPoint)hanging.LocalEndpoint).Port;

        return hanging;
    }
}
