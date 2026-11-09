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
    public async Task<Answer<SiteRead>> ReadAsync(
        string url,
        int maxPages,
        int maxDepth,
        int seconds,
        int maxCharactersEach,
        CancellationToken cancellationToken = default)
    {
        var start = Link.Of(url ?? string.Empty);

        if (Refusal(start, maxPages, maxDepth, seconds, maxCharactersEach) is { } refusal)
            return refusal;

        var scope = crawler.ScopeOf(start) with { MaxPages = maxPages, MaxDepth = maxDepth, Budget = TimeSpan.FromSeconds(seconds) };
        var report = await crawler.CrawlAsync(scope, cancellationToken);
        var walked = Walked(report, maxCharactersEach);

        return report.Stop is CrawlStop.Refused
            ? Answer.Failed(Messages.CrawlStops[CrawlStop.Refused], WhyRefused(report), TypeOf(report)).WithDetails(walked)
            : Answer.Ok(walked);
    }

    private AnswerFailure? Refusal(Link start, int maxPages, int maxDepth, int seconds, int maxCharactersEach)
    {
        if (rules.Refusal(start) is { } inadmissible)
            return inadmissible;

        if (maxPages is < 1 or > Limits.MostSitePages)
            return FailureTypes.Refused(Messages.SitePagesOutOfRange(maxPages));

        if (maxDepth is < 0 or > Limits.MostSiteDepth)
            return FailureTypes.Refused(Messages.SiteDepthOutOfRange(maxDepth));

        if (seconds is < Limits.FewestSiteSeconds or > Limits.MostSiteSeconds)
            return FailureTypes.Refused(Messages.SiteSecondsOutOfRange(seconds));

        return maxCharactersEach is < Limits.FewestCharacters or > Limits.MostCharactersEach
            ? FailureTypes.Refused(Messages.CharactersEachOutOfRange(maxCharactersEach))
            : null;
    }

    /// <summary>The type of a walk refused at its start: the kind of the page that refused it, or an unexpected failure when no page says.</summary>
    private static string TypeOf(CrawlReport report) =>
        FailureTypes.Of(report.Pages.FirstOrDefault(page => !page.IsSuccess)?.Failure?.Kind ?? FailureKind.Unexpected);

    private static string? WhyRefused(CrawlReport report) =>
        report.Pages.FirstOrDefault(page => !page.IsSuccess)?.Failure is { } failure ? Messages.HintFor(failure) : null;

    private static SiteRead Walked(CrawlReport report, int maxCharactersEach) => new(
        report,
        [.. report.Pages.Where(page => page.IsSuccess).Select(page => new SitePage(page.Value, Piece.Cut(page.Value.Text, 0, maxCharactersEach)))],
        [.. report.Pages.Where(page => !page.IsSuccess).Select(page => page.Failure!)]);
}
