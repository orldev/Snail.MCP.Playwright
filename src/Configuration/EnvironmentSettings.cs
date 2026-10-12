namespace Snail.MCP.Playwright.Configuration;

/// <summary>Maps <c>SNAIL_MCP_PLAYWRIGHT_SECTION__FIELD_NAME</c> variables onto configuration keys <c>Section:FieldName</c>.</summary>
/// <remarks>
/// One prefix carries both this server's own settings and the toolkit's: <c>SNAIL_MCP_PLAYWRIGHT_PORT</c>
/// is the server's, <c>SNAIL_MCP_PLAYWRIGHT_WEB__SCRAPING__BROWSER_SERVER</c> is the toolkit's
/// <c>Web:Scraping:BrowserServer</c>. The platform hands a child process only variables under
/// <c>SNAIL_MCP_</c>, so a setting spelled any other way would never arrive. The reading is Snail.Sdk.Mcp's,
/// the same in every Snail server; the prefix is this server's.
/// </remarks>
public static class EnvironmentSettings
{
    /// <summary>What every variable this server reads begins with.</summary>
    public const string Prefix = "SNAIL_MCP_PLAYWRIGHT_";

    /// <summary>The prefixed variables as configuration keys, blank ones left out.</summary>
    public static IEnumerable<KeyValuePair<string, string?>> Read(System.Collections.IDictionary variables) =>
        Snail.Sdk.Mcp.Hosting.EnvironmentSettings.Read(Prefix, variables);

    internal static string ToConfigurationKey(string variable) =>
        Snail.Sdk.Mcp.Hosting.EnvironmentSettings.ToConfigurationKey(variable);
}
