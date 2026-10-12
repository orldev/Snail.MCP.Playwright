using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Playwright.Configuration;

/// <summary>The name and version this server announces, the same in initialize and in playwright_diagnose.</summary>
public static class ThisAssembly
{
    /// <summary>The server's command, the name a client knows it by in <c>initialize</c>; the registry keeps io.github.orldev/snail-mcp-playwright.</summary>
    public static string Name { get; } = ServerIdentity.NameOf(typeof(ThisAssembly).Assembly);

    /// <summary>The version of the build that is running, without the commit a build may append.</summary>
    public static string Version { get; } = ServerIdentity.VersionOf(typeof(ThisAssembly).Assembly);
}
