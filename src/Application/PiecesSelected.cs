using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>What an expression selected in a page, and the page it was selected in.</summary>
/// <param name="Page">The page the markup came from.</param>
/// <param name="Selected">The pieces taken and how many elements matched.</param>
public sealed record PiecesSelected(PageContent Page, Selection Selected);
