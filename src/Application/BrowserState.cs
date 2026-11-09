namespace Snail.MCP.Playwright.Application;

/// <summary>Whether the browser answered when it was asked, and how long the asking took.</summary>
/// <param name="Server">The address of the browser server, or null when the server launches its own.</param>
/// <param name="Engine">The browser engine configured.</param>
/// <param name="Version">The browser's version, or null when it did not answer.</param>
/// <param name="Error">Why the browser could not be reached, or null when it answered.</param>
/// <param name="Took">How long the asking took.</param>
public sealed record BrowserState(string? Server, string Engine, string? Version, string? Error, TimeSpan Took)
{
    /// <summary>Whether the browser answered.</summary>
    public bool IsReachable => Error is null;
}
