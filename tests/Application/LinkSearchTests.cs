using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Domain;
using Snail.Toolkit.Web.Search;

namespace Snail.MCP.Playwright.Tests.Application;

public sealed class LinkSearchTests
{
    private static readonly DateTimeOffset Later = new(2026, 10, 4, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Find_WithEnginesAnswering_ReturnsLinksAndWhatEachEngineDid()
    {
        var search = FakeSearch.Finding(FakeSearch.Result("PostgreSQL: Documentation", "https://www.postgresql.org/docs/"));

        var answer = await new LinkSearch(search, new ServerConfig()).FindAsync(["postgresql documentation"], 10);

        Assert.True(answer.IsSuccess);
        Assert.Equal("https://www.postgresql.org/docs/", answer.Data!["results"]![0]!["url"]!.GetValue<string>());
        Assert.Equal("Bing", answer.Data["engines"]![0]!["engine"]!.GetValue<string>());
        Assert.Null(answer.Data["note"]);
    }

    /// <summary>
    /// The invariant the whole server stands on: a search that hands back text stops a model from going
    /// to the pages. A result is four short fields, and a field added here has to argue with this test.
    /// </summary>
    [Fact]
    public async Task Find_WithEnginesAnswering_NeverReturnsTheContentOfAPage()
    {
        var search = FakeSearch.Finding(FakeSearch.Result("A title", "https://example.org/", new string('s', 5000)));

        var answer = await new LinkSearch(search, new ServerConfig()).FindAsync(["anything"], 10);

        var result = answer.Data!["results"]![0]!.AsObject();

        Assert.Equal(["engine", "snippet", "title", "url"], [.. result.Select(field => field.Key).Order(StringComparer.Ordinal)]);
        Assert.InRange(result["snippet"]!.GetValue<string>().Length, 1, Limits.SnippetCharacters + 1);
    }

    [Fact]
    public async Task Find_WithMoreLinksThanAsked_ReturnsTheLimitAndSaysHowManyWereFound()
    {
        var search = FakeSearch.Finding([.. Enumerable.Range(0, 12).Select(index => FakeSearch.Result($"Result {index}", $"https://example.org/{index}"))]);

        var answer = await new LinkSearch(search, new ServerConfig()).FindAsync(["anything"], 3);

        Assert.Equal(3, answer.Data!["results"]!.AsArray().Count);
        Assert.Equal(12, answer.Data["found"]!.GetValue<int>());
    }

    /// <summary>Nothing found and nobody answered read identically as an empty list and call for opposite reactions.</summary>
    [Fact]
    public async Task Find_WhenEnginesAnsweredAndMatchedNothing_IsAnAnswerThatSaysSo()
    {
        var search = new FakeSearch(new SearchOutcome([], [FakeSearch.Failed("Bing", new SearchFailure(SearchFailureKind.NoResults))]));

        var answer = await new LinkSearch(search, new ServerConfig()).FindAsync(["zxqv"], 10);

        Assert.True(answer.IsSuccess);
        Assert.Empty(answer.Data!["results"]!.AsArray());
        Assert.Contains("nothing matched", answer.Data["note"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Find_WhenNoEngineAnswered_IsAnErrorThatSaysWhenToAskAgain()
    {
        var search = new FakeSearch(new SearchOutcome([], [FakeSearch.Failed("Bing", SearchFailure.Resting("Bing", Later))]));

        var answer = await new LinkSearch(search, new ServerConfig()).FindAsync(["anything"], 10);

        Assert.False(answer.IsSuccess);
        Assert.Contains("no search engine answered", answer.Error, StringComparison.Ordinal);
        Assert.Contains("resting", answer.Hint, StringComparison.Ordinal);
        Assert.Equal("Resting", answer.Data!["engines"]![0]!["failure"]!.GetValue<string>());
        Assert.StartsWith("2026-10-04T12:30:00", answer.Data["engines"]![0]!["opensAt"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Find_AsksTheQueriesOnceEach_WithinTheTimeOneSearchMayTake()
    {
        var search = FakeSearch.Finding();

        await new LinkSearch(search, new ServerConfig { SearchSeconds = 45 }).FindAsync([" first ", "second", "first", " "], 10);

        Assert.Equal(["first", "second"], search.Asked);
        Assert.Equal(TimeSpan.FromSeconds(45), search.Budget);
    }

    /// <summary>A value out of range is refused, never clamped: thirty links returned for two hundred asked is a false statement about the web.</summary>
    [Theory]
    [InlineData(0, 10, "1 to 5 queries")]
    [InlineData(6, 10, "1 to 5 queries")]
    [InlineData(1, 0, "limit is 1 to 30")]
    [InlineData(1, 31, "limit is 1 to 30")]
    public async Task Find_WithACountOutOfRange_IsRefusedWithoutAskingAnyEngine(int queries, int limit, string said)
    {
        var search = FakeSearch.Finding();

        var answer = await new LinkSearch(search, new ServerConfig())
            .FindAsync([.. Enumerable.Range(0, queries).Select(index => $"query {index}")], limit);

        Assert.False(answer.IsSuccess);
        Assert.Contains(said, answer.Error, StringComparison.Ordinal);
        Assert.Empty(search.Asked);
    }

    [Fact]
    public async Task Find_WithAQueryLongerThanAPhrase_IsRefused()
    {
        var search = FakeSearch.Finding();

        var answer = await new LinkSearch(search, new ServerConfig()).FindAsync([new string('q', Limits.MostQueryCharacters + 1)], 10);

        Assert.False(answer.IsSuccess);
        Assert.Empty(search.Asked);
    }
}
