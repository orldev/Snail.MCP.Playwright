using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>An address of a batch that was not read: the site's failure when it was asked, and the sentence that says why.</summary>
/// <param name="Address">The address as it was asked for.</param>
/// <param name="Failure">What the site did, or null when the address was never sent.</param>
/// <param name="Error">Why the page is missing, in a sentence.</param>
public sealed record UnreadPage(Link Address, PageFailure? Failure, string Error);
