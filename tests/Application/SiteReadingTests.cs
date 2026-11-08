using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Tests.Application;

public sealed class SiteReadingTests
{
    private const string Start = "https://docs.example/";

    private static SiteReading Site(FakeCrawler crawler) => new(crawler, new AddressRules(new ServerConfig()));

    private static Task<Answer<JsonNode>> ReadAsync(FakeCrawler crawler, string url = Start, int maxPages = 10, int maxDepth = 2, int seconds = 120, int each = 2000) =>
        Site(crawler).ReadAsync(url, maxPages, maxDepth, seconds, each);

    /// <summary>The toolkit's own ceilings are five hundred pages and forty minutes; one tool call must never inherit them.</summary>
    [Fact]
    public async Task Read_ASite_WalksItWithinTheCeilingsTheCallerStated()
    {
        var crawler = new FakeCrawler(new FakeReader());

        await ReadAsync(crawler, maxPages: 7, maxDepth: 1, seconds: 45);

        Assert.Equal(7, crawler.Walked!.MaxPages);
        Assert.Equal(1, crawler.Walked.MaxDepth);
        Assert.Equal(TimeSpan.FromSeconds(45), crawler.Walked.Budget);
    }

    [Fact]
    public async Task Read_ASite_ReturnsAPieceOfEveryPageAndTheFailuresBesideThem()
    {
        var crawler = new FakeCrawler(new FakeReader())
        {
            Report = scope => FakeCrawler.Reported(
                scope,
                CrawlStop.Exhausted,
                FakeCrawler.Page("https://docs.example/a", new string('a', 3000), depth: 1),
                FakeCrawler.Failure("https://docs.example/b", FailureKind.Navigation, 404))
        };

        var answer = await ReadAsync(crawler, each: 500);

        Assert.True(answer.IsSuccess);
        Assert.Equal(1, answer.Value!["read"]!.GetValue<int>());
        Assert.Equal(500, answer.Value["pages"]![0]!["text"]!.GetValue<string>().Length);
        Assert.Equal(3000, answer.Value["pages"]![0]!["characters"]!["total"]!.GetValue<int>());
        Assert.Equal(404, answer.Value["failed"]![0]!["status"]!.GetValue<int>());
    }

    /// <summary>A walk that stopped early and one that read the whole site look identical from the pages alone.</summary>
    [Theory]
    [InlineData(CrawlStop.Exhausted, "whole of the site")]
    [InlineData(CrawlStop.PageLimit, "a sample, not the site")]
    [InlineData(CrawlStop.TimeLimit, "a sample, not the site")]
    [InlineData(CrawlStop.DepthLimit, "deeper pages were not read")]
    public async Task Read_ASite_SaysWhyTheWalkEndedAndWhatThatMeans(CrawlStop stop, string means)
    {
        var crawler = new FakeCrawler(new FakeReader())
        {
            Report = scope => FakeCrawler.Reported(scope, stop, FakeCrawler.Page("https://docs.example/a", "text"))
        };

        var answer = await ReadAsync(crawler);

        Assert.True(answer.IsSuccess);
        Assert.Equal(stop.ToString(), answer.Value!["stop"]!.GetValue<string>());
        Assert.Contains(means, answer.Value["stopMeans"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Read_ASiteWhoseStartCouldNotBeRead_IsAnErrorThatSaysWhatTheFailureCallsFor()
    {
        var crawler = new FakeCrawler(new FakeReader())
        {
            Report = scope => FakeCrawler.Reported(scope, CrawlStop.Refused, FakeCrawler.Failure(Start, FailureKind.Blocked))
        };

        var answer = await ReadAsync(crawler);

        Assert.False(answer.IsSuccess);
        Assert.Contains("could not be read at all", answer.Error, StringComparison.Ordinal);
        Assert.Contains("another site", answer.Hint, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://minio:9000/", 10, 2, 120, 2000, "not on the public web")]
    [InlineData(Start, 0, 2, 120, 2000, "maxPages is 1 to 50")]
    [InlineData(Start, 51, 2, 120, 2000, "maxPages is 1 to 50")]
    [InlineData(Start, 10, 6, 120, 2000, "maxDepth is 0 to 5")]
    [InlineData(Start, 10, 2, 29, 2000, "seconds is 30 to 600")]
    [InlineData(Start, 10, 2, 601, 2000, "seconds is 30 to 600")]
    [InlineData(Start, 10, 2, 120, 20001, "maxCharactersEach is 500 to 20000")]
    public async Task Read_WithACeilingOutOfRange_IsRefusedWithoutWalking(string url, int maxPages, int maxDepth, int seconds, int each, string said)
    {
        var crawler = new FakeCrawler(new FakeReader());

        var answer = await ReadAsync(crawler, url, maxPages, maxDepth, seconds, each);

        Assert.False(answer.IsSuccess);
        Assert.Contains(said, answer.Error, StringComparison.Ordinal);
        Assert.Null(crawler.Walked);
    }
}
