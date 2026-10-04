using Snail.MCP.Playwright.Configuration;

namespace Snail.MCP.Playwright.Tests.Live;

[Collection(LiveBrowser.Name)]
public sealed class LiveDiagnosticsTests
{
    /// <summary>The user agent this server announces has to be the browser that is really behind it.</summary>
    [LiveFact]
    public async Task Diagnose_WithTheBrowserRunning_ReportsTheVersionTheDefaultsDescribe()
    {
        await using var server = await ListeningServer.StartAsync(LiveFactAttribute.Browser);
        await using var client = await server.ConnectAsync();

        var browser = (await client.CallToolAsync("playwright_diagnose", new Dictionary<string, object?>())).Data()["browser"]!;

        Assert.True(browser["isReachable"]!.GetValue<bool>(), browser["error"]?.GetValue<string>());
        Assert.StartsWith(BrowserDefaults.FirefoxVersion, browser["version"]!.GetValue<string>(), StringComparison.Ordinal);
    }
}
