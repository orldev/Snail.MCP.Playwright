using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>A walk of a site: how it went and why it ended, the text of every page read and every page that failed.</summary>
/// <param name="Report">The walk as the crawler reports it.</param>
/// <param name="Pages">Each page read, with as much of its text as one entry may carry.</param>
/// <param name="Failed">Each page that could not be read.</param>
public sealed record SiteRead(CrawlReport Report, IReadOnlyList<SitePage> Pages, IReadOnlyList<PageFailure> Failed);
