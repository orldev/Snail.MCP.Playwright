using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;
using Snail.Toolkit.Web.Scraping.Agent;

namespace Snail.MCP.Playwright.Tests.Support;

/// <summary>A page agent that remembers the errand it was sent on and answers what the test told it to.</summary>
internal sealed class FakeAgent(Outcome<PageContent, PageFailure>? outcome = null) : IPageAgent
{
    public Errand? Walked { get; private set; }

    public static Outcome<PageContent, PageFailure> Resulting(string url, string text) =>
        Outcome<PageContent, PageFailure>.Success(
            new PageContent(Link.Of(url), "<html></html>", text, DateTimeOffset.UnixEpoch) { Title = "Results", StatusCode = 200 });

    public Task<Outcome<PageContent, PageFailure>> PerformAsync(Errand errand, CancellationToken ct = default)
    {
        Walked = errand;

        return Task.FromResult(outcome ?? Resulting(errand.Address.Text, "the page after the moves"));
    }
}
