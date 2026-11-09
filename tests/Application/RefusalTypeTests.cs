using Snail.MCP.Playwright.Adapters;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Domain;
using Snail.MCP.Playwright.Tools;
using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Sdk.Mcp.Hosting;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;
using Snail.Toolkit.Web.Search;
using Snail.Toolkit.Web.Text;

namespace Snail.MCP.Playwright.Tests.Application;

/// <summary>Every refusal of this server carries a type, the word a program branches on instead of reading the sentence.</summary>
/// <remarks>A failed page kept its kind only in <c>data.kind</c>, spelled as the toolkit's enum, and a call refused for its arguments or its
/// address carried no kind at all; <c>data.kind</c> stays for the clients that read it.</remarks>
public sealed class RefusalTypeTests
{
    private const string Page = "https://example.org/catalogue";

    private static readonly ServerConfig Config = new();

    private static readonly AddressRules Rules = new(Config);

    private static PageReading Reading(FakeReader reader) => new(reader, Rules, new PageMemory(Config, TimeProvider.System));

    [Theory]
    [InlineData(FailureKind.Blocked, "blocked")]
    [InlineData(FailureKind.RobotsDisallowed, "robotsDisallowed")]
    [InlineData(FailureKind.BrowserUnavailable, "browserUnavailable")]
    [InlineData(FailureKind.Timeout, "timeout")]
    public async Task APageThatFailed_IsTypedByItsKind_AndKeepsTheKindInItsData(FailureKind kind, string type)
    {
        var answer = await Reading(new FakeReader().Refusing(Page, kind)).ReadAsync(Page, 0, Limits.DefaultCharacters);

        Assert.Equal(type, answer.Type);
        Assert.Equal(kind.ToString(), answer.Details!["kind"]!.GetValue<string>());
    }

    [Fact]
    public async Task APageThatFailed_CarriesItsTypeAtTheTopOfTheToolsAnswer()
    {
        var refused = Outcome<PageSnapshot, PageFailure>.Failed(new PageFailure(FailureKind.Blocked, "a wall") { Address = Link.Of(Page) });
        var tools = new CloserTools(null!, null!, new PagePictures(new FakeSnapshots(refused), Rules), null!);

        var result = await tools.ScreenshotAsync(Page);

        Assert.Equal("blocked", result.Type());
        Assert.Equal("Blocked", result.Data()!["kind"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("not an address", ToolFailures.Arguments)]
    [InlineData("http://127.0.0.1/admin", "privateAddress")]
    [InlineData("http://intranet/", "privateAddress")]
    public async Task AnAddressThisServerWillNotRead_IsTypedBeforeAnythingIsSent(string url, string type)
    {
        var reader = new FakeReader();

        var answer = await Reading(reader).ReadAsync(url, 0, Limits.DefaultCharacters);

        Assert.Equal(type, answer.Type);
        Assert.Empty(reader.Visited);
    }

    [Fact]
    public async Task ArgumentsOutOfRange_AreTypedArguments_InEveryTool()
    {
        var reader = new FakeReader().Serving(Page, "Wheat 210", html: "<html><body><p>Wheat</p></body></html>");
        var crawler = new FakeCrawler(new FakeReader());
        var extraction = new PageExtraction(Reading(reader), Rules, new MarkupSelection(new TextExtraction()));
        var fields = new PageFields(Reading(reader), Rules, new MarkupSelection(new TextExtraction()));

        var piece = await Reading(reader).ReadAsync(Page, -1, Limits.DefaultCharacters);
        var beyond = await Reading(reader).ReadAsync(Page, 5000, Limits.DefaultCharacters);
        var batch = await new BatchReading(crawler, Rules, new PageMemory(Config, TimeProvider.System)).ReadAsync([], 2000);
        var site = await new SiteReading(crawler, Rules).ReadAsync(Page, 0, 2, 120, 2000);
        var search = await new LinkSearch(FakeSearch.Finding(), Config).FindAsync([], 10);
        var unnamed = await extraction.SelectAsync(Page, null, null, null, Limits.DefaultSelected);
        var invalid = await extraction.SelectAsync(Page, "//[", null, null, Limits.DefaultSelected);
        var noFields = await fields.TakeAsync(Page, []);
        var walk = await new PageWalking(new FakeAgent(), Rules).PerformAsync(Page, [], Limits.DefaultCharacters);

        Assert.All([piece, beyond, batch, site, search, unnamed, invalid, noFields, walk], answer => Assert.Equal(ToolFailures.Arguments, answer.Type));
    }

    [Fact]
    public async Task ASearchNoEngineAnswered_IsTypedUnanswered()
    {
        var search = new FakeSearch(new SearchOutcome([], [FakeSearch.Failed("Bing", new SearchFailure(SearchFailureKind.Faulted))]));

        var answer = await new LinkSearch(search, Config).FindAsync(["anything"], 10);

        Assert.False(answer.IsSuccess);
        Assert.Equal("unanswered", answer.Type);
    }

    [Fact]
    public async Task ABatchWhereNothingWasRead_IsTypedNothingRead()
    {
        var crawler = new FakeCrawler(new FakeReader().Refusing(Page, FailureKind.Blocked));

        var answer = await new BatchReading(crawler, Rules, new PageMemory(Config, TimeProvider.System)).ReadAsync([Page], 2000);

        Assert.False(answer.IsSuccess);
        Assert.Equal("nothingRead", answer.Type);
    }

    [Fact]
    public async Task ASiteWhoseStartRefused_IsTypedByTheKindOfThatRefusal()
    {
        var crawler = new FakeCrawler(new FakeReader())
        {
            Report = scope => FakeCrawler.Reported(scope, CrawlStop.Refused, FakeCrawler.Failure(Page, FailureKind.RobotsDisallowed))
        };

        var answer = await new SiteReading(crawler, Rules).ReadAsync(Page, 10, 2, 120, 2000);

        Assert.Equal("robotsDisallowed", answer.Type);
    }

    [Fact]
    public async Task AWalkThePageDidNotAnswer_IsTypedByTheKindOfTheFailure()
    {
        var failed = Outcome<PageContent, PageFailure>.Failed(new PageFailure(FailureKind.Timeout, "the page did not answer") { Address = Link.Of(Page) });
        var moves = new List<Move> { new("click", Css: "button") };

        var answer = await new PageWalking(new FakeAgent(failed), Rules).PerformAsync(Page, moves, Limits.DefaultCharacters);

        Assert.Equal("timeout", answer.Type);
        Assert.Equal("Timeout", answer.Details!["kind"]!.GetValue<string>());
    }

    /// <summary>A type the instructions do not name is one a model meets without being told what it means.</summary>
    [Fact]
    public void EveryType_IsNamedInTheInstructions()
    {
        string[] own = [ToolFailures.Arguments, FailureTypes.PrivateAddress, FailureTypes.Unanswered, FailureTypes.NothingRead];
        var kinds = Enum.GetValues<FailureKind>().Select(FailureTypes.Of);

        Assert.All(own.Concat(kinds), type => Assert.Contains(type, ServerGuidance.Instructions, StringComparison.Ordinal));
    }
}
