namespace Snail.MCP.Playwright.Tools.Prompts;

/// <summary>What the client is told about this server when it connects.</summary>
/// <remarks>
/// The instructions are read once, before any tool is called, so they carry what a model cannot discover
/// by trying: which question each tool answers, and what each one costs.
/// </remarks>
public static class ServerGuidance
{
    /// <summary>The text that rides along with initialize.</summary>
    public const string Instructions =
        """
        This server reads the public web through a real browser. Every tool is named playwright_*.

        Two questions, two tools, never mixed. playwright_search answers where something is written
        about: queries in, links out, no page content. playwright_fetch answers what a page says: an
        address in, its text out. Search first, then fetch the results worth reading; a snippet is for
        choosing a link, not for answering from.

        Reading comes in three sizes, and the smallest that fits is the right one. playwright_fetch reads
        one page in seconds, and reads on through a long one piece by piece. playwright_fetch_many reads
        several known addresses in one call. playwright_fetch_site walks a whole site and takes minutes:
        it is for a question about a site, never a way to find one page.

        Four more tools are for what plain reading does not give. playwright_extract takes named pieces
        out of a page by CSS or XPath — the rows of a table, the links of a list. playwright_extract_fields
        takes a record of named values, each with selectors tried in order, and says which one matched: for
        a page read again and again, keep the selectors that matched and pass them first next time.
        playwright_perform fills a form in and reads what the page says afterwards. playwright_screenshot returns a picture, for a page
        whose content is drawn rather than written. Each costs more than a read; reach for one only when
        the text of the page was not enough.

        The server is a polite visitor: it spaces its requests to one site, waits as long as a site asks
        it to and does not go around a refusal. So a failure is an answer, not an obstacle. Read its kind
        and its hint, and take another source rather than the same call again.

        playwright_diagnose says whether the browser behind the server answers; call it when a tool
        reports the browser unavailable.

        Every answer is JSON with an ok flag. When ok is false, read error and hint and change the call
        instead of repeating it.
        """;
}
