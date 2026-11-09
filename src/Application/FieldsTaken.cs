using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>A record taken out of a page: every field asked for, in the order asked, found or not.</summary>
/// <param name="Page">The page the record was taken from.</param>
/// <param name="Fields">Each field, with the selector that found it or none.</param>
public sealed record FieldsTaken(PageContent Page, IReadOnlyList<FieldTaken> Fields);
