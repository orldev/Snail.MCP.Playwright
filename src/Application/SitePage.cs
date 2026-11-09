using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>A page of a walked site and the part of its text an entry carries.</summary>
/// <param name="Page">The page as the crawler read it.</param>
/// <param name="Text">Its text from the start, cut to the ceiling of one entry.</param>
public sealed record SitePage(CrawlPage Page, string Text);
