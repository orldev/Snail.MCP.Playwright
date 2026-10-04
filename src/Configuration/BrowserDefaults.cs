namespace Snail.MCP.Playwright.Configuration;

/// <summary>What the toolkit is told when the deployment says nothing: the browser this server is built for.</summary>
/// <remarks>
/// The toolkit's own defaults describe a Chromium it launches itself. This server never launches a
/// browser — it connects to the <c>ghcr.io/orldev/ff-juggler</c> image — and the toolkit refuses to start
/// when the engine and the user agent name different browsers, because a Gecko browser announcing itself
/// as Chrome is a contradiction a site reads in one line. So the engine, the address and the identity are
/// set together here, and all of them describe that one image: Firefox 153 on Linux, in New York.
/// <para>
/// The timezone a page reads is emulated from the fingerprint, not taken from the container; it equals the
/// image's own <c>TZ</c> so that there is one truth instead of two. A deployment that moves the exit to
/// another country moves <c>Timezone</c>, <c>Locale</c> and <c>AcceptLanguage</c> together.
/// </para>
/// </remarks>
public static class BrowserDefaults
{
    /// <summary>The Firefox release inside the image, which the user agent has to name.</summary>
    public const string FirefoxVersion = "153.0";

    /// <summary>The image these defaults describe.</summary>
    public const string Image = $"ghcr.io/orldev/ff-juggler:{FirefoxVersion}-pw1.62-amd64";

    /// <summary>Where the image's browser server listens when it runs on this machine.</summary>
    public const string Server = "ws://localhost:3000/";

    /// <summary>The user agent that browser really sends.</summary>
    public const string UserAgent = $"Mozilla/5.0 (X11; Linux x86_64; rv:{FirefoxVersion}) Gecko/20100101 Firefox/{FirefoxVersion}";

    /// <summary>The settings laid under everything a deployment configures.</summary>
    public static IReadOnlyDictionary<string, string?> Settings { get; } = new Dictionary<string, string?>
    {
        ["Web:Scraping:BrowserServer"] = Server,
        ["Web:Scraping:Engine"] = "Firefox",
        ["Web:Identity:Fingerprints:0:UserAgent"] = UserAgent,
        ["Web:Identity:Fingerprints:0:Locale"] = "en-US",
        ["Web:Identity:Fingerprints:0:Timezone"] = "America/New_York",
        ["Web:Identity:Fingerprints:0:AcceptLanguage"] = "en-US,en;q=0.9"
    };
}
