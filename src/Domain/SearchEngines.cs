namespace Snail.MCP.Playwright.Domain;

/// <summary>The search engines this server can ask, by the names a deployment writes.</summary>
/// <remarks>
/// The names are the toolkit's own provider names, so what a deployment configures, what an answer
/// reports and what the toolkit logs are one word rather than three spellings of it.
/// </remarks>
public static class SearchEngines
{
    /// <summary>Plain HTTP, no browser; refuses recognised operators such as <c>site:</c>.</summary>
    public const string DuckDuckGo = "DuckDuckGo";

    /// <summary>DuckDuckGo read at its site through the browser, for an exit its HTTP endpoint refuses.</summary>
    /// <remarks>
    /// The HTTP endpoint answered nothing from either address it was asked from — every attempt timed out,
    /// 2026-10-04 and 2026-11-10, and a search waited 37 seconds for it — while the site in Firefox answered in 9.
    /// </remarks>
    public const string DuckDuckGoBrowser = "DuckDuckGoBrowser";

    /// <summary>Read through the browser.</summary>
    public const string Bing = "Bing";

    /// <summary>Read through the browser.</summary>
    public const string Brave = "Brave";

    /// <summary>Walked through its own search box; answers only an address it trusts.</summary>
    public const string Google = "Google";

    /// <summary>Every engine there is, in the order they are described.</summary>
    public static IReadOnlyList<string> All { get; } = [DuckDuckGo, DuckDuckGoBrowser, Bing, Brave, Google];

    /// <summary>The engines asked when a deployment names none.</summary>
    /// <remarks>
    /// DuckDuckGo is asked through the browser rather than over HTTP, so every engine needs the browser; an exit
    /// the HTTP endpoint answers names <see cref="DuckDuckGo"/> to search without one. Google is left out on
    /// purpose: it refuses by address rather than by query — on 2026-09-20 a plain control query was answered
    /// <c>/sorry/</c> alongside the one under test — so switching it on is a statement about the exit the
    /// deployment has, not about this server.
    /// </remarks>
    public const string Default = "DuckDuckGoBrowser,Bing,Brave";

    /// <summary>The engines a comma-separated setting names, spelled the way the toolkit spells them.</summary>
    public static IReadOnlyList<string> Named(string setting) =>
    [
        .. setting
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(name => All.FirstOrDefault(known => known.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? name)
            .Distinct(StringComparer.Ordinal)
    ];
}
