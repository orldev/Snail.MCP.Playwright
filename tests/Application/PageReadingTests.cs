using Microsoft.Extensions.Time.Testing;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Domain;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Tests.Application;

public sealed class PageReadingTests
{
    private const string Page = "https://example.org/long";

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));

    private PageReading Reading(FakeReader reader, ServerConfig? config = null) =>
        new(reader, new AddressRules(config ?? new ServerConfig()), new PageMemory(config ?? new ServerConfig(), _clock));

    [Fact]
    public async Task Read_AShortPage_ReturnsAllOfItAndNoNextPiece()
    {
        var reader = new FakeReader(_clock).Serving(Page, "All there is.", "Short");

        var answer = await Reading(reader).ReadAsync(Page, 0, Limits.DefaultCharacters);

        Assert.True(answer.IsSuccess);
        Assert.Equal("All there is.", answer.Value!["text"]!.GetValue<string>());
        Assert.Equal("Short", answer.Value["title"]!.GetValue<string>());
        Assert.Null(answer.Value["next"]);
    }

    /// <summary>A page cut without a word reads as a page that ended.</summary>
    [Fact]
    public async Task Read_APageLongerThanOneAnswer_ReturnsAPieceAndWhereTheNextStarts()
    {
        var reader = new FakeReader(_clock).Serving(Page, new string('a', 1200));

        var answer = await Reading(reader).ReadAsync(Page, 0, 500);

        Assert.Equal(500, answer.Value!["text"]!.GetValue<string>().Length);
        Assert.Equal(500, answer.Value["next"]!.GetValue<int>());
        Assert.Equal(1200, answer.Value["characters"]!["total"]!.GetValue<int>());
    }

    [Fact]
    public async Task Read_ThePiecesInTurn_AddUpToThePage()
    {
        string text = string.Concat(Enumerable.Range(0, 400).Select(index => $"line {index}\n"));
        var reading = Reading(new FakeReader(_clock).Serving(Page, text));

        var pieces = new List<string>();

        for (int? from = 0; from is not null;)
        {
            var answer = await reading.ReadAsync(Page, from.Value, 500);

            pieces.Add(answer.Value!["text"]!.GetValue<string>());
            from = answer.Value["next"]?.GetValue<int>();
        }

        Assert.Equal(text, string.Concat(pieces));
    }

    /// <summary>
    /// Each piece asked for would otherwise be another visit: the site sees one client reloading one page,
    /// and the caller waits at the politeness gate for the privilege.
    /// </summary>
    [Fact]
    public async Task Read_TheNextPieceOfAPage_DoesNotVisitTheSiteAgain()
    {
        var reader = new FakeReader(_clock).Serving(Page, new string('a', 1200));
        var reading = Reading(reader);

        await reading.ReadAsync(Page, 0, 500);
        await reading.ReadAsync(Page, 500, 500);

        Assert.Single(reader.Visited);
    }

    [Fact]
    public async Task Read_APageRememberedTooLongAgo_VisitsTheSiteAgain()
    {
        var reader = new FakeReader(_clock).Serving(Page, "text");
        var reading = Reading(reader);

        await reading.ReadAsync(Page, 0, 500);
        _clock.Advance(TimeSpan.FromMinutes(new ServerConfig().PageMemoryMinutes + 1));
        await reading.ReadAsync(Page, 0, 500);

        Assert.Equal(2, reader.Visited.Count);
    }

    [Fact]
    public async Task Read_WithTheMemorySwitchedOff_VisitsTheSiteEveryTime()
    {
        var reader = new FakeReader(_clock).Serving(Page, "text");
        var reading = Reading(reader, new ServerConfig { PageMemoryMinutes = 0 });

        await reading.ReadAsync(Page, 0, 500);
        await reading.ReadAsync(Page, 0, 500);

        Assert.Equal(2, reader.Visited.Count);
    }

    [Fact]
    public async Task Read_APieceCutBetweenTheHalvesOfOneCharacter_EndsBeforeIt()
    {
        string text = $"{new string('a', 499)}😀{new string('b', 100)}";
        var reading = Reading(new FakeReader(_clock).Serving(Page, text));

        var first = await reading.ReadAsync(Page, 0, 500);
        var second = await reading.ReadAsync(Page, first.Value!["next"]!.GetValue<int>(), 500);

        Assert.Equal(499, first.Value["text"]!.GetValue<string>().Length);
        Assert.StartsWith("😀", second.Value!["text"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Read_APageTheSiteRefused_NamesTheKindAndWhatItCallsFor()
    {
        var reader = new FakeReader(_clock).Refusing(Page, FailureKind.Blocked, retryAfter: TimeSpan.FromSeconds(90));

        var answer = await Reading(reader).ReadAsync(Page, 0, Limits.DefaultCharacters);

        Assert.False(answer.IsSuccess);
        Assert.Equal("Blocked", answer.Details!["kind"]!.GetValue<string>());
        Assert.Equal(90, answer.Details["retryAfterSeconds"]!.GetValue<long>());
        Assert.Contains("another site", answer.Hint, StringComparison.Ordinal);
        Assert.Contains("90 seconds", answer.Hint, StringComparison.Ordinal);
    }

    /// <summary>A 404 is a fact about the address, and retrying it spends time to be told the same thing.</summary>
    [Fact]
    public async Task Read_AnAddressThatDoesNotExist_SaysRetryingWillNotHelp()
    {
        var answer = await Reading(new FakeReader(_clock)).ReadAsync("https://example.org/missing", 0, Limits.DefaultCharacters);

        Assert.False(answer.IsSuccess);
        Assert.Equal(404, answer.Details!["status"]!.GetValue<int>());
        Assert.False(answer.Details["isWorthRetrying"]!.GetValue<bool>());
        Assert.Contains("does not exist", answer.Hint, StringComparison.Ordinal);
    }

    /// <summary>The address comes from a model, and a model can be talked into asking for the deployment's own services.</summary>
    [Theory]
    [InlineData("http://minio:9000/bucket")]
    [InlineData("http://localhost:8080/")]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://10.0.0.5/admin")]
    [InlineData("http://172.20.1.1/")]
    [InlineData("http://192.168.1.1/")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://[::1]/")]
    [InlineData("http://[fd00::1]/")]
    [InlineData("http://vault.service.internal/")]
    public async Task Read_AnAddressInsideTheDeployment_IsRefusedBeforeAnythingIsSent(string url)
    {
        var reader = new FakeReader(_clock);

        var answer = await Reading(reader).ReadAsync(url, 0, Limits.DefaultCharacters);

        Assert.False(answer.IsSuccess);
        Assert.Contains("not on the public web", answer.Error, StringComparison.Ordinal);
        Assert.Empty(reader.Visited);
    }

    [Fact]
    public async Task Read_AnAddressInsideTheDeployment_IsAllowedWhereADeveloperSaidSo()
    {
        var reader = new FakeReader(_clock).Serving("http://localhost:8080/", "a local page");

        var answer = await Reading(reader, new ServerConfig { AllowPrivateAddresses = true }).ReadAsync("http://localhost:8080/", 0, 500);

        Assert.True(answer.IsSuccess);
    }

    [Theory]
    [InlineData("postgresql documentation")]
    [InlineData("ftp://example.org/file")]
    [InlineData("/docs/index.html")]
    [InlineData("")]
    public async Task Read_SomethingThatIsNotAnAddress_IsRefusedAndPointedAtTheSearch(string url)
    {
        var reader = new FakeReader(_clock);

        var answer = await Reading(reader).ReadAsync(url, 0, Limits.DefaultCharacters);

        Assert.False(answer.IsSuccess);
        Assert.Contains("playwright_search", answer.Error, StringComparison.Ordinal);
        Assert.Empty(reader.Visited);
    }

    [Theory]
    [InlineData(-1, 500, "from is a position")]
    [InlineData(0, 499, "maxCharacters is 500 to 100000")]
    [InlineData(0, 100001, "maxCharacters is 500 to 100000")]
    public async Task Read_WithAPieceOutOfRange_IsRefusedWithoutVisiting(int from, int maxCharacters, string said)
    {
        var reader = new FakeReader(_clock).Serving(Page, "text");

        var answer = await Reading(reader).ReadAsync(Page, from, maxCharacters);

        Assert.False(answer.IsSuccess);
        Assert.Contains(said, answer.Error, StringComparison.Ordinal);
        Assert.Empty(reader.Visited);
    }

    [Fact]
    public async Task Read_FromBeyondTheEndOfThePage_SaysHowLongThePageIs()
    {
        var reader = new FakeReader(_clock).Serving(Page, "short");

        var answer = await Reading(reader).ReadAsync(Page, 50, 500);

        Assert.False(answer.IsSuccess);
        Assert.Contains("5 characters long", answer.Error, StringComparison.Ordinal);
    }
}
