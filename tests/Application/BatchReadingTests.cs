using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Domain;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Tests.Application;

public sealed class BatchReadingTests
{
    private static readonly ServerConfig Config = new();

    private readonly PageMemory _memory = new(Config, TimeProvider.System);

    private BatchReading Batch(FakeReader reader) => new(new FakeCrawler(reader), new AddressRules(Config), _memory);

    [Fact]
    public async Task Read_SeveralPages_ReturnsAPieceOfEachInTheOrderAsked()
    {
        var reader = new FakeReader().Serving("https://a.example/", "first page").Serving("https://b.example/", "second page");

        var answer = await Batch(reader).ReadAsync(["https://b.example/", "https://a.example/"], Limits.DefaultCharactersEach);

        Assert.True(answer.IsSuccess);
        Assert.Equal(["second page", "first page"], [.. answer.Data!["pages"]!.AsArray().Select(page => page!["text"]!.GetValue<string>())]);
        Assert.Empty(answer.Data["failed"]!.AsArray());
    }

    /// <summary>One unreachable site must not cost the caller the pages that answered.</summary>
    [Fact]
    public async Task Read_WithOnePageFailing_ReturnsTheOthersAndSaysWhyThatOneFailed()
    {
        var reader = new FakeReader().Serving("https://a.example/", "a page").Refusing("https://b.example/", FailureKind.RobotsDisallowed);

        var answer = await Batch(reader).ReadAsync(["https://a.example/", "https://b.example/"], Limits.DefaultCharactersEach);

        var failed = answer.Data!["failed"]![0]!;

        Assert.True(answer.IsSuccess);
        Assert.Equal(1, answer.Data["read"]!.GetValue<int>());
        Assert.Equal("RobotsDisallowed", failed["kind"]!.GetValue<string>());
        Assert.Contains("robots.txt", failed["hint"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Read_WithEveryPageFailing_IsAnErrorThatStillListsEachReason()
    {
        var reader = new FakeReader().Refusing("https://a.example/", FailureKind.Blocked).Refusing("https://b.example/", FailureKind.Timeout);

        var answer = await Batch(reader).ReadAsync(["https://a.example/", "https://b.example/"], Limits.DefaultCharactersEach);

        Assert.False(answer.IsSuccess);
        Assert.Contains("none of the 2 pages", answer.Error, StringComparison.Ordinal);
        Assert.Equal(["Blocked", "Timeout"], [.. answer.Data!["failed"]!.AsArray().Select(page => page!["kind"]!.GetValue<string>())]);
    }

    [Fact]
    public async Task Read_WithAnAddressInsideTheDeployment_ReadsTheRestAndNeverAsksForThatOne()
    {
        var reader = new FakeReader().Serving("https://a.example/", "a page");

        var answer = await Batch(reader).ReadAsync(["https://a.example/", "http://minio:9000/"], Limits.DefaultCharactersEach);

        Assert.Single(reader.Visited);
        Assert.Contains("not on the public web", answer.Data!["failed"]![0]!["error"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>A page cut short here is read on with playwright_fetch, and that must not be a second visit.</summary>
    [Fact]
    public async Task Read_APageCutShort_LeavesTheRestOfItToBeReadWithoutAnotherVisit()
    {
        var reader = new FakeReader().Serving("https://a.example/", new string('a', 900));
        var batch = Batch(reader);

        var answer = await batch.ReadAsync(["https://a.example/"], 500);
        var rest = await new PageReading(reader, new AddressRules(Config), _memory).ReadAsync("https://a.example/", answer.Data!["pages"]![0]!["next"]!.GetValue<int>(), 500);

        Assert.Equal(400, rest.Data!["text"]!.GetValue<string>().Length);
        Assert.Single(reader.Visited);
    }

    [Fact]
    public async Task Read_TheSameAddressTwiceInOneCall_VisitsItOnce()
    {
        var reader = new FakeReader().Serving("https://a.example/page", "a page");

        var answer = await Batch(reader).ReadAsync(["https://a.example/page", "https://a.example/page#section"], Limits.DefaultCharactersEach);

        Assert.Single(reader.Visited);
        Assert.Equal(1, answer.Data!["asked"]!.GetValue<int>());
    }

    [Theory]
    [InlineData(0, 4000, "1 to 10 addresses")]
    [InlineData(11, 4000, "1 to 10 addresses")]
    [InlineData(1, 499, "maxCharactersEach is 500 to 20000")]
    [InlineData(1, 20001, "maxCharactersEach is 500 to 20000")]
    public async Task Read_WithACountOutOfRange_IsRefusedWithoutVisiting(int addresses, int maxCharactersEach, string said)
    {
        var reader = new FakeReader();

        var answer = await Batch(reader).ReadAsync([.. Enumerable.Range(0, addresses).Select(index => $"https://a.example/{index}")], maxCharactersEach);

        Assert.False(answer.IsSuccess);
        Assert.Contains(said, answer.Error, StringComparison.Ordinal);
        Assert.Empty(reader.Visited);
    }
}
