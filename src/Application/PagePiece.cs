using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>One piece of a page's text and where it stands in the whole.</summary>
/// <param name="Page">The page the piece is cut from.</param>
/// <param name="From">Where the piece really starts, never between the halves of one character.</param>
/// <param name="Text">The characters of the piece.</param>
/// <param name="Next">Where the next piece starts, or null when this one reaches the end.</param>
public sealed record PagePiece(PageContent Page, int From, string Text, int? Next);
