namespace Snail.MCP.Playwright.Domain;

/// <summary>What an expression matched in a page, or why it could not be asked.</summary>
public sealed record Selection(IReadOnlyList<string> Pieces, int Matched, string? Fault = null)
{
    /// <summary>Whether the expression was one the page could be asked.</summary>
    public bool IsValid => Fault is null;

    /// <summary>An expression that is not valid XPath, with what the parser said of it.</summary>
    public static Selection Invalid(string fault) => new([], 0, fault);
}
