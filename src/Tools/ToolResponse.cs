using System.Text.Encodings.Web;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Snail.MCP.Playwright.Tools;

/// <summary>Uniform tool results: the payload as JSON text with an ok flag, a failure flagged with <c>isError</c>.</summary>
/// <remarks>
/// The text keeps the ok flag the model reads; the error flag is what an orchestrator reads, because a
/// failure that looks like a success in the protocol is invisible to anything but the model.
/// <para>
/// There is deliberately no structured copy beside the text. The payload here is the text of a page,
/// and a client that hands the whole result to its model — the platform this server is built for does —
/// would pay for every page twice.
/// </para>
/// </remarks>
public static class ToolResponse
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>An answer as a tool result: its payload, or its reason flagged as an error.</summary>
    public static CallToolResult From(Answer answer)
    {
        if (!answer.IsSuccess)
            return Failure(answer.Error!, answer.Hint, answer.Data);

        return answer.Picture is { } picture ? Pictured(answer.Data!, picture) : Success(answer.Data!);
    }

    /// <summary>A successful result with something to look at: the words first, the image after them.</summary>
    /// <remarks>
    /// The image is a block of its own, never a field of the JSON: a client hands an image block to a model
    /// as an image, and the platform turns one into a file of the task, while base64 inside the text would
    /// be megabytes of characters read as characters.
    /// </remarks>
    public static CallToolResult Pictured(JsonNode data, Picture picture) => new()
    {
        Content =
        [
            .. Success(data).Content,
            ImageContentBlock.FromBytes(picture.Bytes, picture.MediaType)
        ]
    };

    /// <summary>A successful result.</summary>
    public static CallToolResult Success(JsonNode data) => new()
    {
        Content = [new TextContentBlock { Text = new JsonObject { ["ok"] = true, ["data"] = data.DeepClone() }.ToJsonString(Options) }]
    };

    /// <summary>A failed result: flagged for the protocol, explained with a hint for the model.</summary>
    public static CallToolResult Failure(string message, string? hint = null, JsonNode? data = null) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = Told(message, hint, data).ToJsonString(Options) }]
    };

    private static JsonObject Told(string message, string? hint, JsonNode? data) => new()
    {
        ["ok"] = false,
        ["error"] = message,
        ["hint"] = hint,
        ["data"] = data?.DeepClone()
    };
}
