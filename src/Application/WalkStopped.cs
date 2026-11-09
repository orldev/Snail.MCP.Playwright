using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>A walk the site did not answer: the page it stopped on and how.</summary>
/// <param name="Failure">What the site did.</param>
public sealed record WalkStopped(PageFailure Failure);
