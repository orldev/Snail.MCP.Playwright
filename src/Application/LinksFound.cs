using Snail.Toolkit.Web.Search;

namespace Snail.MCP.Playwright.Application;

/// <summary>What a search found: the links offered, how many there were, and what every engine did to find them.</summary>
/// <param name="Offered">The links handed back, no more than the caller's limit.</param>
/// <param name="Found">How many links the engines found in all, before the limit.</param>
/// <param name="IsNothingMatched">Whether the engines answered and matched nothing, as opposed to not answering.</param>
/// <param name="Attempts">Each engine's attempt at each query.</param>
public sealed record LinksFound(IReadOnlyList<SearchResult> Offered, int Found, bool IsNothingMatched, IReadOnlyList<SearchAttempt> Attempts);
