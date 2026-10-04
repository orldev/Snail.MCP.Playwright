namespace Snail.MCP.Playwright.Domain;

/// <summary>One move on a page, as a caller writes it: what to do, where, and what the site must do in response.</summary>
/// <remarks>
/// Everything is a plain word rather than a number or a nested object, because the one who writes it is
/// a model reading a description: <c>{ "action": "type", "label": "Notice number", "value": "2024-001" }</c>.
/// How an element is named is stated — <see cref="Css"/> or <see cref="Label"/>, exactly one — and never
/// inferred from the shape of a string, since which of the two is in use is the fact worth being explicit
/// about.
/// </remarks>
public sealed record Move(
    string Action,
    string? Css = null,
    string? Label = null,
    string? Value = null,
    string? Expect = null,
    string? Pattern = null,
    int? WithinSeconds = null);
