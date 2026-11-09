namespace Snail.MCP.Playwright.Application;

/// <summary>One field of a record: the selector that found it, or none when no selector did.</summary>
/// <param name="Name">The name the value comes back under.</param>
/// <param name="Expression">The selector that matched, or null when the field was found by none.</param>
/// <param name="Pieces">What the selector took.</param>
/// <param name="Matched">How many elements it matched.</param>
/// <param name="IsEveryMatchWanted">Whether the value is every match rather than the first.</param>
public sealed record FieldTaken(string Name, Expression? Expression, IReadOnlyList<string> Pieces, int Matched, bool IsEveryMatchWanted)
{
    /// <summary>Whether a selector of the field matched.</summary>
    public bool IsFound => Expression is not null;
}
