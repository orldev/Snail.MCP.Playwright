using ModelContextProtocol.Protocol;
using Snail.MCP.Playwright.Domain;
using Snail.MCP.Playwright.Tools;

namespace Snail.MCP.Playwright.Tests.Tools;

public sealed class ToolResponseTests
{
    /// <summary>
    /// The image is a block of its own: a client hands an image block to a model as an image and the platform
    /// turns one into a file of the task, while base64 inside the text would be read as megabytes of characters.
    /// </summary>
    [Fact]
    public void From_AnAnswerWithAPicture_PutsTheWordsFirstAndTheImageInABlockOfItsOwn()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0];
        var answer = Answer.Ok(new JsonObject { ["width"] = 1280 }) with { Picture = new Picture(jpeg, "image/jpeg") };

        var result = ToolResponse.From(answer);

        var words = Assert.IsType<TextContentBlock>(result.Content[0]);
        var image = Assert.IsType<ImageContentBlock>(result.Content[1]);

        Assert.DoesNotContain(Convert.ToBase64String(jpeg), words.Text, StringComparison.Ordinal);
        Assert.Equal("image/jpeg", image.MimeType);
        Assert.Equal(jpeg, image.DecodedData.ToArray());
    }

    [Fact]
    public void From_AFailure_IsFlaggedForTheProtocolAndKeepsWhatWasLearned()
    {
        var result = ToolResponse.From(Answer.Stopped(new JsonObject { ["kind"] = "Blocked" }, "a wall", "use another source"));

        var told = JsonNode.Parse(result.Text())!;

        Assert.Equal(true, result.IsError);
        Assert.False(told["ok"]!.GetValue<bool>());
        Assert.Equal("use another source", told["hint"]!.GetValue<string>());
        Assert.Equal("Blocked", told["data"]!["kind"]!.GetValue<string>());
    }
}
