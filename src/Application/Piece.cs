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
    public static Answer Of(PageContent page, int from, int maxCharacters) =>
        from > 0 && from >= page.Text.Length
            ? Answer.Failed(Messages.FromBeyondTheEnd(from, page.Text.Length))
            : Answer.Ok(Described(page, from, maxCharacters));

    /// <summary>The piece as a caller reads it: where the page is, how much of it this is, and the text.</summary>
    public static JsonObject Described(PageContent page, int from, int maxCharacters)
    {
        string text = Cut(page.Text, from, maxCharacters);
        int next = from + text.Length;

        return new JsonObject
        {
            ["url"] = page.Final.Text,
            ["requested"] = page.IsRedirected ? page.Requested.Text : null,
            ["title"] = page.Title,
            ["status"] = page.StatusCode,
            ["fetchedAt"] = page.FetchedAt.ToString("O"),
            ["characters"] = new JsonObject { ["from"] = from, ["returned"] = text.Length, ["total"] = page.Text.Length },
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
}
