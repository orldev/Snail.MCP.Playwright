namespace Snail.MCP.Playwright.Domain;

/// <summary>The numbers the tool descriptions promise, kept where a guard test can hold prose and code to each other.</summary>
/// <remarks>
/// A value outside its range is refused, never clamped in silence: a caller that asked for two hundred
/// results and received thirty without a word has been told something false about the web.
/// </remarks>
public static class Limits
{
    /// <summary>Queries one search may carry.</summary>
    public const int MostQueries = 5;

    /// <summary>Characters one query may run to.</summary>
    public const int MostQueryCharacters = 400;

    /// <summary>Links a search returns when the caller names no number.</summary>
    public const int DefaultResults = 10;

    /// <summary>Links a search may return at most.</summary>
    public const int MostResults = 30;

    /// <summary>Characters of a snippet kept beside a link; the rest of a long one says nothing a title does not.</summary>
    public const int SnippetCharacters = 300;

    /// <summary>Characters of page text one answer carries when the caller names no number.</summary>
    public const int DefaultCharacters = 20_000;

    /// <summary>The smallest piece of a page worth a call.</summary>
    public const int FewestCharacters = 500;

    /// <summary>The largest piece of a page one answer may carry.</summary>
    public const int MostCharacters = 100_000;

    /// <summary>Addresses one call may read at once.</summary>
    public const int MostAddresses = 10;

    /// <summary>Characters of each page a call over several pages carries when the caller names no number.</summary>
    public const int DefaultCharactersEach = 4_000;

    /// <summary>The largest piece of each page a call over several pages may carry.</summary>
    public const int MostCharactersEach = 20_000;

    /// <summary>Pages of a site read when the caller names no number.</summary>
    public const int DefaultSitePages = 10;

    /// <summary>Pages of a site one call may read at most.</summary>
    public const int MostSitePages = 50;

    /// <summary>Links from the start a walk follows when the caller names no number.</summary>
    public const int DefaultSiteDepth = 2;

    /// <summary>Links from the start a walk may follow at most.</summary>
    public const int MostSiteDepth = 5;

    /// <summary>Seconds a walk over a site may take when the caller names no number.</summary>
    public const int DefaultSiteSeconds = 120;

    /// <summary>The shortest walk worth starting: a sitemap and one paced page do not fit in less.</summary>
    public const int FewestSiteSeconds = 30;

    /// <summary>Seconds a walk over a site may take at most.</summary>
    public const int MostSiteSeconds = 600;

    /// <summary>Characters of each page of a site carried when the caller names no number.</summary>
    public const int DefaultSiteCharactersEach = 2_000;

    /// <summary>Pieces an expression returns when the caller names no number.</summary>
    public const int DefaultSelected = 50;

    /// <summary>Pieces an expression may return at most.</summary>
    public const int MostSelected = 200;

    /// <summary>Characters one selected piece may run to; an expression that matched the whole body is a mistake, not a request.</summary>
    public const int SelectedCharacters = 2_000;

    /// <summary>Fields one record may name.</summary>
    public const int MostFields = 30;

    /// <summary>Selectors one field may try, CSS and XPath together.</summary>
    public const int MostCandidates = 5;

    /// <summary>Moves one walk over a page may make.</summary>
    public const int MostMoves = 12;

    /// <summary>Seconds a move's consequence is waited for when the caller names no number.</summary>
    public const int DefaultWithinSeconds = 5;

    /// <summary>Seconds a move's consequence may be waited for at most.</summary>
    public const int MostWithinSeconds = 30;
}
