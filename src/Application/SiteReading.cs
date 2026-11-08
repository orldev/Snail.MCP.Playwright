using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>Reads a site rather than a page: from its own sitemap where it publishes one, link by link where it does not.</summary>
/// <remarks>
/// The expensive question, and bounded on every side because of it: pages, depth and time are all the
/// caller's to state and all capped here. A walk that stopped early and one that read the whole site
/// look identical from the pages alone, so the answer says why it stopped.
/// </remarks>
public sealed class SiteReading(ICrawler crawler, AddressRules rules)
{
    /// <summary>What the site says, as far as the ceilings allowed, and why the walk ended.</summary>
    public async Task<Answer<JsonNode>> ReadAsync(
        string url,
        int maxPages,
        int maxDepth,
        int seconds,
        int maxCharactersEach,
        CancellationToken cancellationToken = default)
    {
        var start = Link.Of(url ?? string.Empty);

        if (Refusal(start, maxPages, maxDepth, seconds, maxCharactersEach) is { } refusal)
            return Answer.Failed(refusal);

        var scope = crawler.ScopeOf(start) with { MaxPages = maxPages, MaxDepth = maxDepth, Budget = TimeSpan.FromSeconds(seconds) };
        var report = await crawler.CrawlAsync(scope, cancellationToken);

        return report.Stop is CrawlStop.Refused
            ? Answer.Failed(Messages.CrawlStops[CrawlStop.Refused], WhyRefused(report), details: Described(report, maxCharactersEach))
            : Answer.Ok<JsonNode>(Described(report, maxCharactersEach));
    }

    private string? Refusal(Link start, int maxPages, int maxDepth, int seconds, int maxCharactersEach)
    {
        if (rules.Refusal(start) is { } inadmissible)
            return inadmissible;

        if (maxPages is < 1 or > Limits.MostSitePages)
            return Messages.SitePagesOutOfRange(maxPages);

        if (maxDepth is < 0 or > Limits.MostSiteDepth)
            return Messages.SiteDepthOutOfRange(maxDepth);

        if (seconds is < Limits.FewestSiteSeconds or > Limits.MostSiteSeconds)
            return Messages.SiteSecondsOutOfRange(seconds);

        return maxCharactersEach is < Limits.FewestCharacters or > Limits.MostCharactersEach
            ? Messages.CharactersEachOutOfRange(maxCharactersEach)
            : null;
    }

    private static string? WhyRefused(CrawlReport report) =>
        report.Pages.FirstOrDefault(page => !page.IsSuccess)?.Failure is { } failure ? Messages.HintFor(failure) : null;

    private static JsonObject Described(CrawlReport report, int maxCharactersEach) => new()
    {
        ["start"] = report.Start.Text,
        ["stop"] = report.Stop.ToString(),
        ["stopMeans"] = Messages.CrawlStops[report.Stop],
        ["isFromSitemap"] = report.IsFromSitemap,
        ["seconds"] = (long)report.Elapsed.TotalSeconds,
        ["read"] = report.Read,
        ["pages"] = new JsonArray([.. report.Pages.Where(page => page.IsSuccess).Select(page => Described(page.Value, maxCharactersEach))]),
        ["failed"] = new JsonArray([.. report.Pages.Where(page => !page.IsSuccess).Select(page => Described(page.Failure!))])
    };

    private static JsonNode Described(CrawlPage page, int maxCharactersEach)
    {
        string text = Piece.Cut(page.Text, 0, maxCharactersEach);

        return new JsonObject
        {
            ["url"] = page.Address.Text,
            ["title"] = page.Title,
            ["depth"] = page.Depth,
            ["characters"] = new JsonObject { ["returned"] = text.Length, ["total"] = page.OriginalLength },
            ["text"] = text
        };
    }

    private static JsonNode Described(PageFailure failure) => new JsonObject
    {
        ["url"] = failure.Address?.Text,
        ["kind"] = failure.Kind.ToString(),
        ["status"] = failure.StatusCode,
        ["error"] = Messages.Said(failure)
    };
}
