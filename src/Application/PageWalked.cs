using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>The page the moves led to and the part of its text one answer carries.</summary>
/// <param name="Page">The page after the moves were made.</param>
/// <param name="Text">Its text from the start, cut to the caller's ceiling.</param>
public sealed record PageWalked(PageContent Page, string Text);
