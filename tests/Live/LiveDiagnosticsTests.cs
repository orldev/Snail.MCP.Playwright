using System.Net;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Extensions;

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

    /// <summary>The knock is a plain request at a WebSocket server; only the real image shows it reacts to one the way the adapter expects.</summary>
    [LiveFact]
    public async Task Health_WithTheBrowserRunning_AnswersOkWithoutRentingABrowser()
    {
        await using var server = await ListeningServer.StartAsync(LiveFactAttribute.Browser);
        using var http = new HttpClient();

        var answer = await http.GetAsync(new Uri($"{server.Address}{McpServerHost.HealthPath}"));

        Assert.True(answer.StatusCode == HttpStatusCode.OK, await Told(answer));
    }

    /// <summary>Who answered and with what, for a run that was not 200: the server's name, the headers that name a proxy or a login, the body.</summary>
    private static async Task<string> Told(HttpResponseMessage answer)
    {
        var headers = answer.Headers.Concat(answer.Content.Headers).Select(header => $"{header.Key}: {string.Join(", ", header.Value)}");

        return $"{(int)answer.StatusCode} {answer.ReasonPhrase} from {answer.RequestMessage?.RequestUri}; {string.Join("; ", headers)}; body: {await answer.Content.ReadAsStringAsync()}";
    }
}
