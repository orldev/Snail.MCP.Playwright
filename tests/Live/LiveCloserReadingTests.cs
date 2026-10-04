using ModelContextProtocol.Protocol;

namespace Snail.MCP.Playwright.Tests.Live;

/// <summary>Pieces, a picture and a filled-in form, against the real web through the real browser.</summary>
[Collection(LiveBrowser.Name)]
public sealed class LiveCloserReadingTests
{
    [LiveFact]
    public async Task Extract_FromARealPage_ReturnsTheElementTheExpressionNames()
    {
        await using var server = await ListeningServer.StartAsync(LiveFactAttribute.Browser);
        await using var client = await server.ConnectAsync();

        var answer = await client.CallToolAsync(
            "playwright_extract",
            new Dictionary<string, object?> { ["url"] = "https://www.iana.org/help/example-domains", ["xpath"] = "//h1" });

        Assert.NotEqual(true, answer.IsError);
        Assert.Equal("Example Domains", answer.Data()["pieces"]![0]!.GetValue<string>());
    }

    [LiveFact]
    public async Task Screenshot_OfARealPage_ReturnsAJpegBesideItsDescription()
    {
        await using var server = await ListeningServer.StartAsync(LiveFactAttribute.Browser);
        await using var client = await server.ConnectAsync();

        var answer = await client.CallToolAsync("playwright_screenshot", new Dictionary<string, object?> { ["url"] = "https://example.com/" });

        var image = Assert.IsType<ImageContentBlock>(answer.Content[1]);

        Assert.NotEqual(true, answer.IsError);
        Assert.Equal("image/jpeg", image.MimeType);
        Assert.Equal([0xFF, 0xD8, 0xFF], image.DecodedData[..3].ToArray());
        Assert.Equal("Example Domain", answer.Data()["title"]!.GetValue<string>());
    }

    /// <summary>The defect this server found in the toolkit on its first day: every article was called a wall.</summary>
    [LiveFact]
    public async Task Fetch_OfAWikipediaArticle_ReadsItRatherThanCallingItAWall()
    {
        await using var server = await ListeningServer.StartAsync(LiveFactAttribute.Browser);
        await using var client = await server.ConnectAsync();

        var answer = await client.CallToolAsync(
            "playwright_fetch",
            new Dictionary<string, object?> { ["url"] = "https://en.wikipedia.org/wiki/PostgreSQL", ["maxCharacters"] = 2000 });

        Assert.NotEqual(true, answer.IsError);
        Assert.Contains("PostgreSQL", answer.Data()["title"]!.GetValue<string>(), StringComparison.Ordinal);
    }
}
