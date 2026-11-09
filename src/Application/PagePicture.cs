using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>A picture of a page and whether the whole page was asked for.</summary>
/// <param name="Snapshot">The picture and where it came from.</param>
/// <param name="IsWholePage">Whether the caller asked for the whole page rather than the screen.</param>
public sealed record PagePicture(PageSnapshot Snapshot, bool IsWholePage);
