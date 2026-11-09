using Snail.MCP.Playwright.Adapters;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Domain;
using Snail.MCP.Playwright.Tools;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;
using Snail.Toolkit.Web.Scraping.Agent;
using Snail.Toolkit.Web.Text;

namespace Snail.MCP.Playwright.Tests.Application;

/// <summary>What plain reading does not give: pieces by expression, a picture, and a page after a form was filled in.</summary>
public sealed class CloserReadingTests
{
    private const string Page = "https://example.org/catalogue";

    private const string Markup = "<html><body><table><tr><td>Wheat</td><td>210</td></tr></table></body></html>";

    private static readonly ServerConfig Config = new();

    private static readonly AddressRules Rules = new(Config);

    private static (PageExtraction Extraction, PageReading Reading) Extraction(FakeReader reader)
    {
        var reading = new PageReading(reader, Rules, new PageMemory(Config, TimeProvider.System));

        return (new PageExtraction(reading, Rules, new MarkupSelection(new TextExtraction())), reading);
    }

    private static PageFields Fields(FakeReader reader) =>
        new(new PageReading(reader, Rules, new PageMemory(Config, TimeProvider.System)), Rules, new MarkupSelection(new TextExtraction()));

    [Fact]
    public async Task Extract_FromAPage_ReturnsThePiecesTheExpressionNames()
    {
        var (extraction, _) = Extraction(new FakeReader().Serving(Page, "Wheat 210", html: Markup));

        var answer = await extraction.SelectAsync(Page, "//tr", null, null, Limits.DefaultSelected);

        Assert.True(answer.TryGet(out var selected));
        Assert.Equal(["Wheat 210"], selected.Selected.Pieces);
        Assert.Equal(1, selected.Selected.Matched);
    }

    /// <summary>A caller reads a page, sees a table in its text and asks for the rows: that is one visit, not two.</summary>
    [Fact]
    public async Task Extract_FromAPageThatWasJustRead_DoesNotVisitTheSiteAgain()
    {
        var reader = new FakeReader().Serving(Page, "Wheat 210", html: Markup);
        var (extraction, reading) = Extraction(reader);

        await reading.ReadAsync(Page, 0, Limits.DefaultCharacters);
        await extraction.SelectAsync(Page, "//td", null, null, Limits.DefaultSelected);

        Assert.Single(reader.Visited);
    }

    [Fact]
    public async Task Extract_WithAnExpressionMatchingNothing_IsAnAnswerThatSaysSo()
    {
        var (extraction, _) = Extraction(new FakeReader().Serving(Page, "Wheat 210", html: Markup));

        var answer = await extraction.SelectAsync(Page, "//article", null, null, Limits.DefaultSelected);

        Assert.True(answer.TryGet(out var selected));
        Assert.Contains("matched nothing", AnswerFields.Of(selected)["note"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Page, "//table[@id=", 50, "not a valid XPath expression")]
    [InlineData(Page, " ", 50, "exactly one of the two")]
    [InlineData(Page, "//tr", 0, "limit is 1 to 200")]
    [InlineData(Page, "//tr", 201, "limit is 1 to 200")]
    [InlineData("http://minio:9000/", "//tr", 50, "not on the public web")]
    public async Task Extract_AskedWrongly_IsRefusedAndSaysHow(string url, string xpath, int limit, string said)
    {
        var (extraction, _) = Extraction(new FakeReader().Serving(Page, "Wheat 210", html: Markup));

        var answer = await extraction.SelectAsync(url, xpath, null, null, limit);

        Assert.False(answer.IsSuccess);
        Assert.Contains(said, answer.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Extract_ByCss_ReturnsThePiecesTheSelectorNames()
    {
        var (extraction, _) = Extraction(new FakeReader().Serving(Page, "Wheat 210", html: Markup));

        var answer = await extraction.SelectAsync(Page, null, "td", null, Limits.DefaultSelected);

        Assert.True(answer.TryGet(out var selected));
        Assert.Equal(["Wheat", "210"], selected.Selected.Pieces);
    }

    /// <summary>Which language an expression is in is said, never guessed: both at once is a question with two answers.</summary>
    [Fact]
    public async Task Extract_NamingCssAndXPathAtOnce_IsRefused()
    {
        var (extraction, _) = Extraction(new FakeReader().Serving(Page, "Wheat 210", html: Markup));

        var answer = await extraction.SelectAsync(Page, "//td", "td", null, Limits.DefaultSelected);

        Assert.Contains("exactly one of the two", answer.Error, StringComparison.Ordinal);
    }

    /// <summary>
    /// A site read every month is rebuilt now and then: the selector of last time misses, the broader one behind it
    /// finds the value, and the answer names it so the caller can keep it.
    /// </summary>
    [Fact]
    public async Task Fields_FirstSelectorMissesAndTheSecondMatches_TakesTheValueAndNamesTheSelector()
    {
        var fields = Fields(new FakeReader().Serving(Page, "Wheat 210", html: Markup));

        var answer = await fields.TakeAsync(Page, [new Field("price", Css: ["td.price", "tr td:nth-child(2)"])]);

        Assert.True(answer.TryGet(out var taken));
        var record = AnswerFields.Of(taken);

        Assert.Equal("210", record["fields"]!["price"]!.GetValue<string>());
        Assert.Equal("tr td:nth-child(2)", record["found"]![0]!["css"]!.GetValue<string>());
        Assert.Null(record["missing"]);
    }

    [Fact]
    public async Task Fields_NoSelectorMatches_ListsTheFieldAsMissingAndSaysWhy()
    {
        var fields = Fields(new FakeReader().Serving(Page, "Wheat 210", html: Markup));

        var answer = await fields.TakeAsync(Page, [new Field("grain", Css: ["td:first-child"]), new Field("stock", Css: [".stock"], XPath: ["//span[@class='stock']"])]);

        Assert.True(answer.TryGet(out var taken));
        var record = AnswerFields.Of(taken);

        Assert.Equal("Wheat", record["fields"]!["grain"]!.GetValue<string>());
        Assert.Equal(["stock"], [.. taken.Fields.Where(field => !field.IsFound).Select(field => field.Name)]);
        Assert.Contains("rebuilt", record["note"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Fields_ManyWanted_ReturnsEveryMatch()
    {
        var fields = Fields(new FakeReader().Serving(Page, "Wheat 210", html: Markup));

        var answer = await fields.TakeAsync(Page, [new Field("cells", XPath: ["//td"], IsEveryMatchWanted: true)]);

        Assert.True(answer.TryGet(out var taken));
        Assert.Equal(["Wheat", "210"], AnswerFields.Of(taken)["fields"]!["cells"]!.AsArray().Select(cell => cell!.GetValue<string>()));
    }

    [Fact]
    public async Task Fields_ASelectorThatDoesNotParse_StopsTheRecordAndNamesTheField()
    {
        var fields = Fields(new FakeReader().Serving(Page, "Wheat 210", html: Markup));

        var answer = await fields.TakeAsync(Page, [new Field("price", Css: ["td[id="])]);

        Assert.StartsWith("field 'price': 'td[id=' is not a valid CSS selector", answer.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Fields_TwoOfOneName_AreRefusedBeforeThePageIsRead()
    {
        var reader = new FakeReader().Serving(Page, "Wheat 210", html: Markup);

        var answer = await Fields(reader).TakeAsync(Page, [new Field("price", Css: ["td"]), new Field("price", Css: ["tr"])]);

        Assert.Contains("two fields are named 'price'", answer.Error, StringComparison.Ordinal);
        Assert.Empty(reader.Visited);
    }

    [Fact]
    public async Task Screenshot_OfAPage_CarriesThePictureBesideItsDescription()
    {
        var snapshots = new FakeSnapshots();

        var taken = await new PagePictures(snapshots, Rules).TakeAsync(Page, wholePage: false);

        Assert.True(taken.TryGet(out var picture));
        Assert.Equal(FakeSnapshots.Jpeg, picture.Snapshot.Image.ToArray());
        Assert.Equal(1280, AnswerFields.Of(picture)["width"]!.GetValue<int>());
        Assert.Equal(SnapshotFrame.Screen, snapshots.Framed);
    }

    /// <summary>A picture that stops without saying so reads as a page that ended.</summary>
    [Fact]
    public async Task Screenshot_OfAScreenWithMorePageBelowIt_SaysHowToSeeTheRest()
    {
        Assert.True((await new PagePictures(new FakeSnapshots(), Rules).TakeAsync(Page, wholePage: false)).TryGet(out var picture));
        var described = AnswerFields.Of(picture);

        Assert.True(described["isCut"]!.GetValue<bool>());
        Assert.Contains("whole page", described["note"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Screenshot_OfTheWholePage_AsksTheCameraForIt()
    {
        var snapshots = new FakeSnapshots();

        await new PagePictures(snapshots, Rules).TakeAsync(Page, wholePage: true);

        Assert.Equal(SnapshotFrame.WholePage, snapshots.Framed);
    }

    [Fact]
    public async Task Screenshot_OfAPageTheSiteRefused_IsTheRefusalAndNoPicture()
    {
        var refused = Outcome<PageSnapshot, PageFailure>.Failed(new PageFailure(FailureKind.Blocked, "a wall") { Address = Link.Of(Page) });

        var taken = await new PagePictures(new FakeSnapshots(refused), Rules).TakeAsync(Page, wholePage: false);

        Assert.False(taken.TryGet(out _));
        Assert.True(taken.TryGetDetails<PageFailure>(out var failure));
        Assert.Equal(FailureKind.Blocked, failure.Kind);
    }

    [Fact]
    public async Task Screenshot_OfAnAddressInsideTheDeployment_IsRefusedWithoutAVisit()
    {
        var snapshots = new FakeSnapshots();

        var taken = await new PagePictures(snapshots, Rules).TakeAsync("http://minio:9000/", wholePage: false);

        Assert.False(taken.IsSuccess);
        Assert.Null(snapshots.Framed);
    }

    [Fact]
    public async Task Perform_WithMovesWrittenAsWords_WalksThePageWithTheStepsTheyDescribe()
    {
        var agent = new FakeAgent();

        var answer = await new PageWalking(agent, Rules).PerformAsync(
            Page,
            [
                new Move("TYPE", Label: "Notice number", Value: "2024-001", Expect: "request", Pattern: "suggest.json", WithinSeconds: 12),
                new Move("press", Css: "#q"),
                new Move("click", Css: "#look", Expect: "Text", Pattern: "is registered")
            ],
            Limits.DefaultCharacters);

        var steps = agent.Walked!.Steps;

        Assert.True(answer.IsSuccess);
        Assert.Equal([StepKind.Type, StepKind.Press, StepKind.Click], [.. steps.Select(step => step.Kind)]);
        Assert.Equal(Spot.Labelled("Notice number"), steps[0].Where);
        Assert.Equal(new Effect(EffectKind.Request, "suggest.json") { Within = TimeSpan.FromSeconds(12) }, steps[0].Expected);
        Assert.Equal(Spot.Css("#q"), steps[1].Where);
        Assert.Null(steps[1].Expected);
        Assert.Equal(TimeSpan.FromSeconds(Limits.DefaultWithinSeconds), steps[2].Expected!.Within);
    }

    /// <summary>
    /// The page after the moves is an answer to those moves; kept under its address it would be handed to the
    /// next plain read of that address as though it were the page.
    /// </summary>
    [Fact]
    public async Task Perform_ReturnsThePageThatResulted_AndCutsItWithoutPromisingMore()
    {
        var agent = new FakeAgent(FakeAgent.Resulting(Page, new string('r', 900)));

        var answer = await new PageWalking(agent, Rules).PerformAsync(Page, [new Move("click", Css: "#go")], 500);

        Assert.True(answer.TryGet(out var walked));
        var described = AnswerFields.Of(walked);

        Assert.Equal(500, walked.Text.Length);
        Assert.True(described["isCut"]!.GetValue<bool>());
        Assert.False(described.ContainsKey("next"));
    }

    /// <summary>The toolkit describes the page a move failed on, and the next attempt is written from that description.</summary>
    [Fact]
    public async Task Perform_WithAMoveTheSiteIgnored_KeepsTheToolkitsDescriptionOfThePage()
    {
        const string described = "Step 1 produced nothing.\nThe address matched 0 elements.\nFields offered: 'query', 'region'.";
        var agent = new FakeAgent(Outcome<PageContent, PageFailure>.Failed(new PageFailure(FailureKind.Empty, described) { Address = Link.Of(Page) }));

        var answer = await new PageWalking(agent, Rules).PerformAsync(Page, [new Move("click", Css: "#go")], 500);

        Assert.False(answer.IsSuccess);
        Assert.Contains("Fields offered: 'query', 'region'.", answer.Error, StringComparison.Ordinal);
        Assert.Contains("rewrite the move", answer.Hint, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("dance", "#x", null, null, null, null, null, "is not a move; the moves are type, pick, click, press, choose")]
    [InlineData("2", "#x", null, null, null, null, null, "is not a move")]
    [InlineData("click", null, null, null, null, null, null, "exactly one of the two")]
    [InlineData("click", "#x", "Caption", null, null, null, null, "exactly one of the two")]
    [InlineData("type", "#x", null, null, null, null, null, "type needs a value")]
    [InlineData("choose", "#x", null, "", null, null, null, "choose needs a value")]
    [InlineData("click", "#x", null, null, "request", null, null, "expect and pattern go together")]
    [InlineData("click", "#x", null, null, null, "results", null, "expect and pattern go together")]
    [InlineData("click", "#x", null, null, "explodes", "x", null, "is not a reaction; the reactions are request, appears, vanishes, text")]
    [InlineData("click", "#x", null, null, "text", "done", 0, "withinSeconds is 1 to 30")]
    [InlineData("click", "#x", null, null, "text", "done", 31, "withinSeconds is 1 to 30")]
    public async Task Perform_WithAMoveWrittenWrongly_NamesTheMoveAndTheMistakeWithoutOpeningThePage(
        string action, string? css, string? label, string? value, string? expect, string? pattern, int? within, string said)
    {
        var agent = new FakeAgent();

        var answer = await new PageWalking(agent, Rules).PerformAsync(
            Page,
            [new Move("click", Css: "#fine"), new Move(action, css, label, value, expect, pattern, within)],
            Limits.DefaultCharacters);

        Assert.False(answer.IsSuccess);
        Assert.StartsWith("move 2:", answer.Error, StringComparison.Ordinal);
        Assert.Contains(said, answer.Error, StringComparison.Ordinal);
        Assert.Null(agent.Walked);
    }

    [Theory]
    [InlineData(0, "1 to 12 moves")]
    [InlineData(13, "1 to 12 moves")]
    public async Task Perform_WithACountOfMovesOutOfRange_IsRefused(int moves, string said)
    {
        var agent = new FakeAgent();

        var answer = await new PageWalking(agent, Rules).PerformAsync(
            Page,
            [.. Enumerable.Range(0, moves).Select(_ => new Move("click", Css: "#go"))],
            Limits.DefaultCharacters);

        Assert.False(answer.IsSuccess);
        Assert.Contains(said, answer.Error, StringComparison.Ordinal);
        Assert.Null(agent.Walked);
    }
}
