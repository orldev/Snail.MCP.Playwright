using System.ComponentModel;
using Microsoft.Extensions.Options;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
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
        Assert.False(answer.Data!["browser"]!["isReachable"]!.GetValue<bool>());
        Assert.Contains("Permission denied", answer.Data["browser"]!["error"]!.GetValue<string>(), StringComparison.Ordinal);
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

    private static Health Health(IBrowserPool pool) =>
        new(pool, [], Options.Create(new ScrapingOptions()), new ServerConfig(), TimeProvider.System);

    private sealed class BrokenPool(Exception failure) : IBrowserPool
    {
        public Task<IBrowserLease> RentAsync(CancellationToken ct = default) => Task.FromException<IBrowserLease>(failure);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
