using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Tests.Tools;

public sealed class MessagesTests
{
    /// <summary>
    /// Measured on 2026-10-04: a host that does not resolve came back as «NS_ERROR_UNKNOWN_HOST» followed by
    /// the browser's call log, which is a trace for the toolkit's maintainer and noise in a model's context.
    /// </summary>
    [Fact]
    public void Said_OfAFailureWithACallLog_KeepsTheReasonAndDropsTheLog()
    {
        var failure = new PageFailure(FailureKind.Navigation, "NS_ERROR_UNKNOWN_HOST\nCall log:\n  - navigating to \"https://nonexistent.invalid/x\"");

        Assert.Equal("Navigation: NS_ERROR_UNKNOWN_HOST", Messages.Said(failure));
    }

    [Fact]
    public void Said_OfAFailureWithAStatus_NamesIt() =>
        Assert.Equal("Navigation (404): not found", Messages.Said(new PageFailure(FailureKind.Navigation, "not found") { StatusCode = 404 }));

    [Theory]
    [InlineData(null, "could not be reached")]
    [InlineData(404, "does not exist")]
    [InlineData(403, "anonymous visitor")]
    [InlineData(429, "Come back later")]
    [InlineData(500, "on its own side")]
    public void Hint_ForANavigationFailure_FollowsTheStatusTheSiteAnsweredWith(int? status, string said) =>
        Assert.Contains(said, Messages.HintFor(new PageFailure(FailureKind.Navigation, "failed") { StatusCode = status }), StringComparison.Ordinal);
}
