namespace Snail.MCP.Playwright.Configuration;

/// <summary>This server's own settings; the toolkit's live beside them under <c>Web:</c>.</summary>
public sealed class ServerConfig
{
    /// <summary>The section a settings file may nest these under.</summary>
    public const string SectionName = "Snail";

    /// <summary>Port to answer on; none means the server is started as a program and speaks stdio.</summary>
    /// <remarks>
    /// One knob decides the whole shape of the process, because the two shapes serve different callers:
    /// at an address it stands beside the browser and behind every worker of a machine, as a program it
    /// belongs to the one client that started it.
    /// </remarks>
    public int Port { get; set; }

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
}
