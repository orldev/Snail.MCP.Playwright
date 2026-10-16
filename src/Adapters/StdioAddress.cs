namespace Snail.MCP.Playwright.Adapters;

/// <summary>A server started as a program has no address, and says so by answering nothing.</summary>
public sealed class StdioAddress : IRequestAddress
{
    /// <inheritdoc/>
    public IReadOnlyList<string> Values(string parameter) => [];
}
