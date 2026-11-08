namespace Snail.MCP.Playwright.Configuration;

/// <summary>This server's own settings; the toolkit's live beside them under <c>Web:</c>.</summary>
public sealed class ServerConfig
{
    /// <summary>The section a settings file may nest these under.</summary>
    public const string SectionName = "Snail";

    /// <summary>The search engines to ask, comma-separated: DuckDuckGo, Bing, Brave, Google.</summary>
    public string Engines { get; set; } = SearchEngines.Default;

    /// <summary>Seconds <c>playwright_diagnose</c> waits for the browser before calling it unreachable.</summary>
    public int BrowserProbeSeconds { get; set; } = 10;

    /// <summary>Seconds one search may take; an engine that does not open inside them is not asked and says so.</summary>
    /// <remarks>
    /// The engines differ by orders of magnitude — one answers at a request a second, another at one
    /// every two minutes — and a tool call that waits for the slowest holds the whole step of an agent.
    /// </remarks>
    public int SearchSeconds { get; set; } = 60;

    /// <summary>Whether addresses inside the deployment's own network may be read; off unless a developer turns it on.</summary>
    /// <remarks>
    /// The address comes from a model, and a model can be talked into asking for <c>http://minio:9000</c>.
    /// The browser is kept out of the internal network by the deployment, but <c>robots.txt</c> and the
    /// sitemap are fetched by this process, which stands in that network — so the refusal is made here,
    /// before anything is sent.
    /// </remarks>
    public bool AllowPrivateAddresses { get; set; }

    /// <summary>Minutes a page that was read is remembered, so the rest of it is served without a second visit; 0 forgets at once.</summary>
    public int PageMemoryMinutes { get; set; } = 10;

    /// <summary>Pages remembered at once; the one read longest ago gives way.</summary>
    public int PageMemoryPages { get; set; } = 16;

    /// <summary>Megabytes of markup and text the remembered pages may hold together; the oldest give way first, and the page read last is kept whatever its size.</summary>
    /// <remarks>
    /// A ceiling in pages alone is no ceiling: the toolkit cuts neither the markup nor the text of a page, and
    /// 32 ordinary heavy pages held 257 MB on 2026-10-16 — half of what the deployment gives the whole server.
    /// </remarks>
    public int PageMemoryMegabytes { get; set; } = 64;
}
