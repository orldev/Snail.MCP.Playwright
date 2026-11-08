using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>One piece of a page's text, with what a caller needs to ask for the next.</summary>
/// <remarks>
/// A page of five megabytes must not land in a model's context whole, and a page cut without a word
/// reads as a page that ended. So a piece says where it stands in the whole: how much there is, how
/// much of it this is, and the position the next piece starts at.
/// </remarks>
public static class Piece
{
    /// <summary>The piece of <paramref name="page"/> that starts at <paramref name="from"/>, or why there is none.</summary>
    public static Answer<JsonNode> Of(PageContent page, int from, int maxCharacters) =>
        from > 0 && from >= page.Text.Length
            ? Answer.Failed(Messages.FromBeyondTheEnd(from, page.Text.Length))
            : Answer.Ok<JsonNode>(Described(page, from, maxCharacters));

    /// <summary>The piece as a caller reads it: where the page is, how much of it this is, and the text.</summary>
    public static JsonObject Described(PageContent page, int from, int maxCharacters)
    {
        int start = Whole(page.Text, from);
        string text = Cut(page.Text, start, maxCharacters);
        int next = start + text.Length;

        return new JsonObject
        {
            ["url"] = page.Final.Text,
            ["requested"] = page.IsRedirected ? page.Requested.Text : null,
            ["title"] = page.Title,
            ["status"] = page.StatusCode,
            ["fetchedAt"] = page.FetchedAt.ToString("O"),
            ["characters"] = new JsonObject { ["from"] = start, ["returned"] = text.Length, ["total"] = page.Text.Length },
            ["next"] = next < page.Text.Length ? next : null,
            ["text"] = text
        };
    }

    /// <summary>The characters of <paramref name="text"/> from <paramref name="from"/>, never ending between the halves of one character.</summary>
    public static string Cut(string text, int from, int maxCharacters)
    {
        int end = Math.Min(text.Length, from + maxCharacters);

        if (end > from && end < text.Length && char.IsHighSurrogate(text[end - 1]))
            end--;

        return text[from..end];
    }

    /// <summary>The position itself, or one earlier when it points between the halves of one character.</summary>
    /// <remarks>
    /// <c>next</c> is always placed before a pair, but <c>from</c> is any number a model writes, and a piece opening
    /// with the low half of a character was serialised as U+FFFD — the character lost, the pieces no longer adding
    /// up to the page. Found by the audit of 2026-10-16; <c>characters.from</c> reports where the piece really starts.
    /// </remarks>
    private static int Whole(string text, int from) =>
        from > 0 && from < text.Length && char.IsLowSurrogate(text[from]) ? from - 1 : from;
}
