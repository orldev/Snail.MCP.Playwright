using ModelContextProtocol.Protocol;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Tools;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Tests.Tools;

/// <summary>The one tool whose answer has more than words: the envelope first, the picture in a block of its own after it.</summary>
public sealed class ScreenshotAnswerTests
{
    private const string Page = "https://example.org/catalogue";

    private static readonly AddressRules Rules = new(new ServerConfig());

    private static CloserTools Tools(FakeSnapshots snapshots) => new(null!, null!, new PagePictures(snapshots, Rules), null!);

    /// <summary>
    /// The image is a block of its own: a client hands an image block to a model as an image and the platform
    /// turns one into a file of the task, while base64 inside the text would be read as megabytes of characters.
    /// </summary>
    [Fact]
    public async Task Screenshot_OfAPage_PutsTheWordsFirstAndTheImageInABlockOfItsOwn()
    {
        var result = await Tools(new FakeSnapshots()).ScreenshotAsync(Page);

        var words = Assert.IsType<TextContentBlock>(result.Content[0]);
        var image = Assert.IsType<ImageContentBlock>(result.Content[1]);

        Assert.True(result.IsOk(), words.Text);
        Assert.Equal(1280, result.Data()!["width"]!.GetValue<int>());
        Assert.DoesNotContain(Convert.ToBase64String(FakeSnapshots.Jpeg), words.Text, StringComparison.Ordinal);
        Assert.Equal(PageSnapshot.MediaType, image.MimeType);
        Assert.Equal(FakeSnapshots.Jpeg, image.DecodedData.ToArray());
    }

    /// <summary>The page is paid for once: a structured copy beside the text would hand a model every answer twice.</summary>
    [Fact]
    public async Task Screenshot_OfAPage_CarriesItsDescriptionInTheTextAlone()
    {
        var result = await Tools(new FakeSnapshots()).ScreenshotAsync(Page);

        Assert.Null(result.StructuredContent);
    }

    [Fact]
    public async Task Screenshot_OfAPageTheSiteRefused_IsFlaggedForTheProtocolKeepsWhatWasLearnedAndHasNoPicture()
    {
        var refused = Outcome<PageSnapshot, PageFailure>.Failed(new PageFailure(FailureKind.Blocked, "a wall") { Address = Link.Of(Page) });

        var result = await Tools(new FakeSnapshots(refused)).ScreenshotAsync(Page);

        Assert.Equal(true, result.IsError);
        Assert.False(result.IsOk());
        Assert.NotNull(result.Hint());
        Assert.Equal("Blocked", result.Data()!["kind"]!.GetValue<string>());
        Assert.Single(result.Content);
    }
}
