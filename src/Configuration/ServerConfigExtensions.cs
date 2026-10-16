using System.Collections;
using Microsoft.Extensions.Configuration;
using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Playwright.Configuration;

/// <summary>Builds the settings the server runs on: the browser defaults, then the settings file, then the variables.</summary>
public static class ServerConfigExtensions
{
    /// <summary>The server's short name: its settings file is <c>~/.config/s3l-mcp-playwright/config.json</c>, its variables begin with <c>SNAIL_MCP_PLAYWRIGHT_</c>.</summary>
    /// <remarks>
    /// One prefix carries both this server's own settings and the toolkit's: <c>SNAIL_MCP_PLAYWRIGHT_PORT</c>
    /// is the server's, <c>SNAIL_MCP_PLAYWRIGHT_WEB__SCRAPING__BROWSER_SERVER</c> is the toolkit's
    /// <c>Web:Scraping:BrowserServer</c>, and the settings file holds the same keys.
    /// </remarks>
    public const string Name = "playwright";

    /// <summary>Everything configured, for this server and for the toolkit beneath it: the first settings file of Snail.Sdk's chain, then the variables, which win.</summary>
    /// <remarks>
    /// Built apart from the host's own configuration on purpose: an <c>appsettings.json</c> lying in the
    /// working directory of whoever started the server, or an unprefixed variable of theirs, is not a
    /// setting of this server and must not become one by accident.
    /// </remarks>
    public static IConfiguration Settings() => Settings(ServerSettings.FileOf(Name), Environment.GetEnvironmentVariables());

    /// <summary>The browser defaults under the settings of <paramref name="file"/>, when there is one, under the server's variables among <paramref name="variables"/>.</summary>
    public static IConfiguration Settings(string? file, IDictionary variables) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(BrowserDefaults.Settings)
            .AddServerSettings(Name, file, variables)
            .Build();

    /// <summary>What the settings say the server is, read before there is a host to read it into.</summary>
    /// <remarks>
    /// Read first and separately because the answer decides which host is built at all: a port means a
    /// web host listening for sessions, no port means a console speaking stdio to its parent.
    /// </remarks>
    public static ServerConfig Read(IConfiguration settings)
    {
        var config = new ServerConfig();

        settings.GetSection(ServerConfig.SectionName).Bind(config);
        settings.Bind(config);

        Validate(config);

        return config;
    }

    /// <summary>Refuses a setting outside its range at start-up, so a typo stops the process instead of being repaired in silence.</summary>
    internal static void Validate(ServerConfig config)
    {
        var faults = new List<string>();

        if (config.Port is < 0 or > 65535)
            faults.Add($"Port must be between 0 and 65535, not {config.Port}");

        if (config.BrowserProbeSeconds is < 1 or > 120)
            faults.Add($"BrowserProbeSeconds must be between 1 and 120, not {config.BrowserProbeSeconds}");

        if (config.SearchSeconds is < 5 or > 300)
            faults.Add($"SearchSeconds must be between 5 and 300, not {config.SearchSeconds}");

        if (config.PageMemoryMinutes is < 0 or > 120)
            faults.Add($"PageMemoryMinutes must be between 0 and 120, not {config.PageMemoryMinutes}");

        if (config.PageMemoryPages is < 1 or > 256)
            faults.Add($"PageMemoryPages must be between 1 and 256, not {config.PageMemoryPages}");

        if (config.PageMemoryMegabytes is < 1 or > 1024)
            faults.Add($"PageMemoryMegabytes must be between 1 and 1024, not {config.PageMemoryMegabytes}");

        if (SearchEngines.Named(config.Engines) is { Count: 0 })
            faults.Add("Engines names no search engine, which leaves playwright_search with nobody to ask");

        if (SearchEngines.Named(config.Engines).Except(SearchEngines.All, StringComparer.Ordinal).ToList() is { Count: > 0 } unknown)
            faults.Add($"Engines names {string.Join(", ", unknown)}, and the engines there are: {string.Join(", ", SearchEngines.All)}");

        if (faults.Count > 0)
            throw new InvalidOperationException($"invalid configuration: {string.Join("; ", faults)}");
    }
}
