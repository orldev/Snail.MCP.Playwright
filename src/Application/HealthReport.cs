using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Playwright.Application;

/// <summary>The state of the server, of the browser behind it and of the search it offers.</summary>
/// <param name="Name">The server's name.</param>
/// <param name="Version">The server's version.</param>
/// <param name="Transport">How the server is spoken to.</param>
/// <param name="Browser">Whether the browser answers.</param>
/// <param name="Engines">The search engines the server asks.</param>
public sealed record HealthReport(string Name, string Version, McpTransport Transport, BrowserState Browser, IReadOnlyList<string> Engines);
