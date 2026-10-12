using System.Text.Json.Serialization;

namespace Snail.MCP.Playwright.Domain;

/// <summary>One named value to take out of a page, with every way of finding it the caller knows of.</summary>
/// <remarks>
/// Several selectors, tried in order — CSS first, then XPath — because a site that is read every month is
/// rebuilt now and then: the selector that worked last time goes first, and a broader one stands behind it.
/// The answer names the one that matched, so the caller can remember it for the next time.
/// </remarks>
/// <param name="Name">What the value is called in the answer.</param>
/// <param name="Css">CSS selectors to try, in order.</param>
/// <param name="XPath">XPath expressions to try after the CSS ones, in order.</param>
/// <param name="Attribute">The attribute to take instead of the text, such as href.</param>
/// <param name="IsEveryMatchWanted">Whether every match is wanted rather than the first; <c>many</c> on the wire.</param>
public sealed record Field(string Name, string[]? Css = null, string[]? XPath = null, string? Attribute = null, [property: JsonPropertyName("many")] bool IsEveryMatchWanted = false)
{
    /// <summary>The expressions to try, in the order they are tried.</summary>
    public IReadOnlyList<Expression> Candidates =>
    [
        .. (Css ?? []).Where(text => !string.IsNullOrWhiteSpace(text)).Select(Expression.Css),
        .. (XPath ?? []).Where(text => !string.IsNullOrWhiteSpace(text)).Select(Expression.XPath)
    ];
}
