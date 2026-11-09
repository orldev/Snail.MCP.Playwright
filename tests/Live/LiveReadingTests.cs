using Snail.MCP.Playwright.Domain;

namespace Snail.MCP.Playwright.Tests.Live;

/// <summary>The questions asked of the real web, through the real browser.</summary>
[Collection(LiveBrowser.Name)]
public sealed class LiveReadingTests
{
    [LiveFact]
    public async Task Fetch_OfARealPage_ReturnsItsTitleAndText()
    {
        await using var server = await ListeningServer.StartAsync(LiveFactAttribute.Browser);
        await using var client = await server.ConnectAsync();

        var answer = await client.CallToolAsync("playwright_fetch", new Dictionary<string, object?> { ["url"] = "https://example.com/" });

        Assert.NotEqual(true, answer.IsError);
        Assert.Equal("Example Domain", answer.Data()!["title"]!.GetValue<string>());
        Assert.Contains("documentation", answer.Data()!["text"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// What is held to here is the shape, not the web: some engine answers, and what it offers are links.
    /// Which engine answers from which address is a fact that changes by the day.
    /// </summary>
    [LiveFact]
    public async Task Search_OfTheRealWeb_ReturnsLinksFromAnEngineThatAnswered()
    {
        await using var server = await ListeningServer.StartAsync(LiveFactAttribute.Browser);
        await using var client = await server.ConnectAsync();

        var answer = await client.CallToolAsync(
            "playwright_search",
            new Dictionary<string, object?> { ["queries"] = new[] { "postgresql documentation" }, ["limit"] = 5 });

        Assert.NotEqual(true, answer.IsError);
        Assert.NotEmpty(answer.Data()!["results"]!.AsArray());
        Assert.All(answer.Data()!["results"]!.AsArray(), result => Assert.StartsWith("http", result!["url"]!.GetValue<string>(), StringComparison.Ordinal));
    }

    /// <summary>
    /// DuckDuckGo read at its site through the browser, the one engine asked: links back mean it answered, from an
    /// address whose HTTP endpoint times out.
    /// </summary>
    [LiveFact]
    public async Task Search_OfDuckDuckGoThroughTheBrowser_ReturnsLinks()
    {
        await using var server = await ListeningServer.StartAsync([.. LiveFactAttribute.Browser, ("ENGINES", SearchEngines.DuckDuckGoBrowser)]);
        await using var client = await server.ConnectAsync();

        var answer = await client.CallToolAsync(
            "playwright_search",
            new Dictionary<string, object?> { ["queries"] = new[] { "postgresql documentation" }, ["limit"] = 5 });

        Assert.True(answer.IsOk(), answer.Text());
        Assert.NotEmpty(answer.Data()!["results"]!.AsArray());
    }
}
