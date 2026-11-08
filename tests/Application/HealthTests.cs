using System.ComponentModel;
using Microsoft.Extensions.Options;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Application.Ports;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Extensions;
using Snail.Sdk.Mcp.Hosting;
using Snail.Toolkit.Web.Scraping;
using Snail.Toolkit.Web.Scraping.Browsers;

namespace Snail.MCP.Playwright.Tests.Application;

public sealed class HealthTests
{
    /// <summary>
    /// Found on 2026-10-04 in the first image built: the driver's node binary was not executable by the
    /// container's user, and the one tool meant to explain a dead browser answered with a stack trace.
    /// </summary>
    [Fact]
    public async Task Report_WhenTheDriverCannotEvenStart_SaysSoInsteadOfThrowing()
    {
        var health = Health(new BrokenPool(new Win32Exception(13, "Permission denied")));

        var answer = await health.ReportAsync();

        Assert.True(answer.IsSuccess);
        Assert.False(answer.Value!["browser"]!["isReachable"]!.GetValue<bool>());
        Assert.Contains("Permission denied", answer.Value["browser"]!["error"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>The caller changing its mind is not a state of the browser, and must not be reported as one.</summary>
    [Fact]
    public async Task Report_WhenTheCallerCancels_PropagatesTheCancellation()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var health = Health(new BrokenPool(new OperationCanceledException(cancelled.Token)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => health.ReportAsync(cancelled.Token));
    }

    /// <summary>A rent waits for a permit the page loads in flight hold; the health check must not.</summary>
    [Fact]
    public async Task IsBrowserReachable_WithABrowserServerConfigured_KnocksInsteadOfRenting()
    {
        var pool = new BrokenPool(new InvalidOperationException("a rent was asked for"));
        var health = Health(pool, new Door(isAnswering: true), "ws://browser:3000/");

        Assert.True(await health.IsBrowserReachableAsync());
    }

    [Fact]
    public async Task IsBrowserReachable_WhenNobodyAnswersTheKnock_IsFalse()
    {
        var health = Health(new BrokenPool(new InvalidOperationException("a rent was asked for")), new Door(isAnswering: false), "ws://browser:3000/");

        Assert.False(await health.IsBrowserReachableAsync());
    }

    /// <summary>A server that launches its own browser has no address to knock at, and is asked the only way there is.</summary>
    [Fact]
    public async Task IsBrowserReachable_WithNoBrowserServerConfigured_Rents()
    {
        var health = Health(new BrokenPool(new Win32Exception(13, "Permission denied")), new Door(isAnswering: true), browserServer: null);

        Assert.False(await health.IsBrowserReachableAsync());
    }

    private static readonly McpEndpoint Stdio = new(McpTransport.Stdio, McpServerHost.DefaultUrl, ListensOnEveryInterface: false);

    private static Health Health(IBrowserPool pool) => Health(pool, new Door(isAnswering: false), browserServer: null);

    private static Health Health(IBrowserPool pool, IBrowserServer door, string? browserServer) =>
        new(pool, door, [], Options.Create(new ScrapingOptions { BrowserServer = browserServer }), new ServerConfig(), Stdio, TimeProvider.System);

    private sealed class Door(bool isAnswering) : IBrowserServer
    {
        public Task<bool> IsAnsweringAsync(TimeSpan patience, CancellationToken ct = default) => Task.FromResult(isAnswering);
    }

    private sealed class BrokenPool(Exception failure) : IBrowserPool
    {
        public Task<IBrowserLease> RentAsync(CancellationToken ct = default) => Task.FromException<IBrowserLease>(failure);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
