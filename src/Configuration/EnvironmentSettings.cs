namespace Snail.MCP.Playwright.Configuration;

/// <summary>Maps <c>SNAIL_MCP_PLAYWRIGHT_SECTION__FIELD_NAME</c> variables onto configuration keys <c>Section:FieldName</c>.</summary>
/// <remarks>
/// One prefix carries both this server's own settings and the toolkit's: <c>SNAIL_MCP_PLAYWRIGHT_PORT</c>
/// is the server's, <c>SNAIL_MCP_PLAYWRIGHT_WEB__SCRAPING__BROWSER_SERVER</c> is the toolkit's
/// <c>Web:Scraping:BrowserServer</c>. The platform hands a child process only variables under
/// <c>SNAIL_MCP_</c>, so a setting spelled any other way would never arrive.
/// </remarks>
public static class EnvironmentSettings
{
    /// <summary>What every variable this server reads begins with.</summary>
    public const string Prefix = "SNAIL_MCP_PLAYWRIGHT_";

    /// <summary>The prefixed variables as configuration keys, blank ones left out.</summary>
    public static IEnumerable<KeyValuePair<string, string?>> Read(System.Collections.IDictionary variables)
    {
        foreach (System.Collections.DictionaryEntry entry in variables)
        {
            if (entry.Key is not string name || !name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (entry.Value is not string value || string.IsNullOrWhiteSpace(value))
                continue;

            yield return new KeyValuePair<string, string?>(ToConfigurationKey(name[Prefix.Length..]), value.Trim());
        }
    }

    internal static string ToConfigurationKey(string variable) =>
        string.Join(':', variable.Split("__").Select(segment => segment.Replace("_", string.Empty, StringComparison.Ordinal)));
}
