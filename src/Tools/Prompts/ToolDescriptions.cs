namespace Snail.MCP.Playwright.Tools.Prompts;

/// <summary>What the model reads before calling: the tool contract, guarded by the tests in <c>ToolCatalogTests</c>.</summary>
/// <remarks>
/// Separate from anything a human reads, because these strings steer behaviour: a limit stated here is a
/// promise about the code, and a guard test fails when the two drift apart. Every word is also paid for
/// in every session — the list is read into the context before anything is asked — so a description
/// says what a model cannot find out by trying, and nothing else.
/// </remarks>
public static class ToolDescriptions
{
    /// <summary><c>playwright_diagnose</c></summary>
    public const string Diagnose =
        """
        Say what state this server is in: its version, whether the browser it reads pages through answers,
        and which search engines it asks. Call it when a tool reports the browser unavailable, not before
        every task.
        """;

    /// <summary><c>playwright_search</c></summary>
    public const string Search =
        """
        Find where something is written about: queries in, links out — title, address, a short snippet and
        the engine that offered each. It never returns the content of a page; read a result with
        playwright_fetch. Several phrasings of one question in a single call cost less than several calls.
        The engines field says what each engine did: an empty list when engines answered means nothing
        matched, while no engine answering is an error that says when to ask again.
        """;

    /// <summary>The <c>queries</c> of <c>playwright_search</c>.</summary>
    public const string SearchQueries =
        "1 to 5 search queries, each up to 400 characters. Plain words work with every engine; operators such as site: or filetype: only with the engines read through the browser.";

    /// <summary>The <c>limit</c> of <c>playwright_search</c>.</summary>
    public const string SearchLimit = "How many links to return, 1 to 30; 10 by default.";

    /// <summary><c>playwright_fetch</c></summary>
    public const string Fetch =
        """
        Read one page: its address in, its title and readable text out, loaded by a real browser so a
        page built by script is read the way a person sees it. It takes an address, never a query. A long
        text comes in pieces: next is the position to pass as from for the following piece, and null when
        the page has ended; the pieces of a page read within 10 minutes cost the site a single visit. A
        failure names its kind — blocked, forbidden by robots.txt, empty, timed out — and says what to do
        instead of repeating the call.
        """;

    /// <summary>The <c>url</c> of <c>playwright_fetch</c>.</summary>
    public const string FetchUrl = "Absolute http or https address of a public page.";

    /// <summary>The <c>from</c> of <c>playwright_fetch</c>.</summary>
    public const string FetchFrom = "Position in the text to start at: 0 by default, or the next of the previous piece.";

    /// <summary>The <c>maxCharacters</c> of <c>playwright_fetch</c>.</summary>
    public const string FetchCharacters = "Characters of text to return, 500 to 100000; 20000 by default.";

    /// <summary><c>playwright_fetch_many</c></summary>
    public const string FetchMany =
        """
        Read several pages in one call: addresses in, a piece of each page's text out, and a page that
        failed listed beside the others rather than failing the call. Use it after a search, to read the
        results worth reading at once. A page cut short carries next: pass it to playwright_fetch as from
        to read on.
        """;

    /// <summary>The <c>urls</c> of <c>playwright_fetch_many</c>.</summary>
    public const string FetchManyUrls = "1 to 10 absolute http or https addresses of public pages.";

    /// <summary>The <c>maxCharactersEach</c> of <c>playwright_fetch_many</c>.</summary>
    public const string FetchManyCharacters = "Characters of text to return for each page, 500 to 20000; 4000 by default.";

    /// <summary><c>playwright_fetch_site</c></summary>
    public const string FetchSite =
        """
        Read a site rather than a page: one address to start from, and the pages of that site are found in
        its own sitemap, or by following its links when it publishes none. This is the expensive tool —
        tens of seconds to minutes, one paced visit per page — so use it only when the question is about a
        site as a whole, and playwright_fetch_many when the addresses are already known. The answer says
        why it stopped: any stop but Exhausted means the site has more than was read.
        """;

    /// <summary>The <c>url</c> of <c>playwright_fetch_site</c>.</summary>
    public const string FetchSiteUrl = "Address to start from, which also decides the site: pages on other hosts are not read.";

    /// <summary>The <c>maxPages</c> of <c>playwright_fetch_site</c>.</summary>
    public const string FetchSitePages = "Pages to read at most, 1 to 50; 10 by default.";

    /// <summary>The <c>maxDepth</c> of <c>playwright_fetch_site</c>.</summary>
    public const string FetchSiteDepth = "Links from the start to follow when the site has no sitemap, 0 to 5; 2 by default.";

    /// <summary>The <c>seconds</c> of <c>playwright_fetch_site</c>.</summary>
    public const string FetchSiteSeconds = "Seconds the walk may take, 30 to 600; 120 by default. What was read when they run out is returned.";

    /// <summary>The <c>maxCharactersEach</c> of <c>playwright_fetch_site</c>.</summary>
    public const string FetchSiteCharacters = "Characters of text to return for each page, 500 to 20000; 2000 by default.";

    /// <summary><c>playwright_extract</c></summary>
    public const string Extract =
        """
        Take named pieces out of a page instead of its whole text: an XPath in, the text of every element
        it matches out — or one attribute of each, such as href. For the rows of a table, the links of a
        list, the prices of a catalogue: what playwright_fetch returns as one long text. The page is the one
        playwright_fetch read; asking about it again within 10 minutes costs the site no further visit.
        """;

    /// <summary>The <c>url</c> of <c>playwright_extract</c>.</summary>
    public const string ExtractUrl = "Absolute http or https address of a public page.";

    /// <summary>The <c>xpath</c> of <c>playwright_extract</c>.</summary>
    public const string ExtractXPath = "XPath 1.0 naming the elements, over the rendered markup with lower-case names: //table[@id='prices']//tr, //h2, //a[contains(@href,'/docs/')].";

    /// <summary>The <c>attribute</c> of <c>playwright_extract</c>.</summary>
    public const string ExtractAttribute = "Attribute to return instead of the text, such as href or src; a relative address comes back absolute. Omit it for the text.";

    /// <summary>The <c>limit</c> of <c>playwright_extract</c>.</summary>
    public const string ExtractLimit = "Pieces to return at most, 1 to 200; 50 by default.";

    /// <summary><c>playwright_screenshot</c></summary>
    public const string Screenshot =
        """
        Look at a page instead of reading it: an address in, a picture of what the browser drew out. For
        what text cannot carry — a chart, a map, a layout, a page whose content is an image. It is slower
        and heavier than playwright_fetch and says nothing a picture does not show, so read first and look
        only when the text is not enough.
        """;

    /// <summary>The <c>url</c> of <c>playwright_screenshot</c>.</summary>
    public const string ScreenshotUrl = "Absolute http or https address of a public page.";

    /// <summary>The <c>wholePage</c> of <c>playwright_screenshot</c>.</summary>
    public const string ScreenshotWholePage = "True to picture the page from top to bottom, up to 8000 pixels; false, the default, pictures the screen a visitor first sees.";

    /// <summary><c>playwright_perform</c></summary>
    public const string Perform =
        """
        Use a page instead of only reading it: open an address, make a few moves on it, and get the text of
        the page that results. For what a page says only after a form is filled in — a registry search, a
        filter, a site's own search box. A move the site did not react to is reported with what the page
        offered instead: rewrite the move from that rather than repeat it. It does not sign in and does not
        solve a challenge.
        """;

    /// <summary>The <c>url</c> of <c>playwright_perform</c>.</summary>
    public const string PerformUrl = "Absolute http or https address of the public page to start on.";

    /// <summary>The <c>moves</c> of <c>playwright_perform</c>.</summary>
    public const string PerformMoves =
        """
        1 to 12 moves, made in order. action: type (key by key into a field), pick (press the offered
        suggestion whose text matches value), choose (an option of a real select list, by its visible text),
        click, or press (a key while the element has the focus; Enter unless value names another). Name the
        element one way: css, a selector, or label, the visible caption of a field. expect with pattern
        holds a move to the site's reaction: request (the page asked for a URL containing pattern), appears
        or vanishes (an element matching the selector in pattern), text (the page came to contain the
        phrase). withinSeconds is how long that reaction is waited for, 1 to 30; 5 by default.
        """;

    /// <summary>The <c>maxCharacters</c> of <c>playwright_perform</c>.</summary>
    public const string PerformCharacters = "Characters of the resulting page's text to return, 500 to 100000; 20000 by default.";
}
