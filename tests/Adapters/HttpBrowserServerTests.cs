using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using Snail.MCP.Playwright.Adapters;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Tests.Adapters;

/// <summary>The knock at the browser server's door, against every kind of door there is.</summary>
public sealed class HttpBrowserServerTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(2);

    [Theory]
    [InlineData("ws://browser:3000/", "http://browser:3000/")]
    [InlineData("wss://browser.example/pw", "https://browser.example/pw")]
    [InlineData("http://browser:3000/", "http://browser:3000/")]
    public void Knockable_OfTheBrowserServersAddress_IsTheSamePlaceOverHttp(string configured, string knockable) =>
        Assert.Equal(new Uri(knockable), HttpBrowserServer.Knockable(configured));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not an address")]
    public void Knockable_OfNoAddress_IsNull(string? configured) =>
        Assert.Null(HttpBrowserServer.Knockable(configured));

    /// <summary>A WebSocket server answers a plain request with a refusal, and a refusal is an answer.</summary>
    [Fact]
    public async Task IsAnswering_WhenTheServerRepliesToTheKnock_IsTrue()
    {
        using var door = Door(async client =>
        {
            await client.GetStream().WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 426 Upgrade Required\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"));
            client.Close();
        }, out int port);

        Assert.True(await Knock(port).IsAnsweringAsync(Patience));
    }

    /// <summary>A server that hangs up on seeing the request is still a server.</summary>
    [Fact]
    public async Task IsAnswering_WhenTheServerClosesOnSeeingTheRequest_IsTrue()
    {
        using var door = Door(client =>
        {
            client.Close();

            return Task.CompletedTask;
        }, out int port);

        Assert.True(await Knock(port).IsAnsweringAsync(Patience));
    }

    [Fact]
    public async Task IsAnswering_WhenNobodyListens_IsFalse()
    {
        using var door = Door(_ => Task.CompletedTask, out int port);
        door.Stop();

        Assert.False(await Knock(port).IsAnsweringAsync(Patience));
    }

    [Fact]
    public async Task IsAnswering_WhenTheServerAcceptsAndNeverAnswers_IsFalseWithinThePatience()
    {
        var held = new List<TcpClient>();
        using var door = Door(client =>
        {
            held.Add(client);

            return Task.CompletedTask;
        }, out int port);
        long asked = Stopwatch.GetTimestamp();

        bool isAnswering = await Knock(port).IsAnsweringAsync(Patience);

        Assert.False(isAnswering);
        Assert.InRange(Stopwatch.GetElapsedTime(asked).TotalSeconds, 0, 10);
    }

    [Fact]
    public async Task IsAnswering_WhenTheCallerCancels_PropagatesTheCancellation()
    {
        var held = new List<TcpClient>();
        using var door = Door(client =>
        {
            held.Add(client);

            return Task.CompletedTask;
        }, out int port);
        using var cancelled = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Knock(port).IsAnsweringAsync(Patience, cancelled.Token));
    }

    private static HttpBrowserServer Knock(int port) =>
        new(Options.Create(new ScrapingOptions { BrowserServer = $"ws://127.0.0.1:{port}/" }));

    private static TcpListener Door(Func<TcpClient, Task> answering, out int port)
    {
        var door = new TcpListener(IPAddress.Loopback, 0);
        door.Start();
        port = ((IPEndPoint)door.LocalEndpoint).Port;
        _ = Task.Run(async () =>
        {
            while (true)
                await answering(await door.AcceptTcpClientAsync());
        });

        return door;
    }
}
