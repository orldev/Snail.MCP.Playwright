using Snail.MCP.Playwright.Configuration;
using Snail.Sdk.Mcp.Hosting;
using Snail.Toolkit.Web.Scraping;
using Snail.Toolkit.Web.Scraping.Agent;
using Snail.Toolkit.Web.Search;

namespace Snail.MCP.Playwright.Tools.Prompts;

/// <summary>What a caller is told when something did not work: the reason, and the next move.</summary>
/// <remarks>
/// Kept apart from <see cref="ToolDescriptions"/> because the audience differs. A description is read
/// before a call and steers what is asked; a message is read after one and has to end the guessing —
/// which is why every failure kind the toolkit can report has its own advice here, and a guard test
/// fails when the toolkit grows a kind this file does not answer.
/// </remarks>
public static class Messages
{
    /// <summary>What is said when every engine was asked and none answered.</summary>
    public const string NobodyAnswered = "no search engine answered, so this says nothing about whether the web has it";

    /// <summary>What is said when the engines answered and offered nothing.</summary>
    public const string NothingMatched = "the engines answered and nothing matched: reword the query rather than repeat it";

    /// <summary>How a search is asked, for a caller that asked it wrongly.</summary>
    public const string SearchHint = "Pass queries as a list of plain phrases, and limit as a number of links.";

    /// <summary>What each way a page read can fail calls for; navigation failures are told apart by status instead.</summary>
    public static IReadOnlyDictionary<FailureKind, string> PageHints { get; } = new Dictionary<FailureKind, string>
    {
        [FailureKind.Blocked] = "The site answered with a challenge instead of the page. Repeating the call at once only repeats the refusal: look for the same content on another site.",
        [FailureKind.Timeout] = "The page did not load in time. One more attempt is reasonable; a second timeout means the site is slow or down.",
        [FailureKind.Navigation] = "The page could not be opened.",
        [FailureKind.BrowserUnavailable] = "The browser behind this server does not answer. Call playwright_diagnose; the page itself was never asked.",
        [FailureKind.Proxy] = "The exit this server was configured with could not be used. This is the deployment's to fix, and no call will get past it.",
        [FailureKind.RobotsDisallowed] = "The site's robots.txt forbids this address. Do not retry and do not look for a way around it: use another source.",
        [FailureKind.Empty] = "The page loaded and holds no text. That is a fact about the page, not a failure: retrying returns the same nothing.",
        [FailureKind.Unreadable] = "The page loaded, but its markup is nested too deeply to be parsed and was refused. It cannot be read through this server.",
        [FailureKind.Unexpected] = "Something failed that has no better name. One more attempt is reasonable; if it fails the same way, use another source."
    };

    /// <summary>What each way an engine can fail to answer calls for.</summary>
    public static IReadOnlyDictionary<SearchFailureKind, string> SearchHints { get; } = new Dictionary<SearchFailureKind, string>
    {
        [SearchFailureKind.NoResults] = "An engine answered and matched nothing.",
        [SearchFailureKind.Faulted] = "An engine could not be reached or its page could not be read; if every engine faults, call playwright_diagnose, because the browser may be down.",
        [SearchFailureKind.Canceled] = "An engine did not answer within its own timeout.",
        [SearchFailureKind.Unreadable] = "An engine answered with markup too deeply nested to parse.",
        [SearchFailureKind.Resting] = "An engine refused a moment ago and is resting; opensAt says when it may be asked again, and asking sooner only prolongs the rest.",
        [SearchFailureKind.OutOfTime] = "An engine's gate does not open within the time one search may take; opensAt says when it does.",
        [SearchFailureKind.Refused] = "An engine refused the query or answered about something else. Operators such as site: or filetype: are refused by an engine asked without a browser: reword without them."
    };

    /// <summary>What each way a walk over a site can end means for how far its answer can be trusted.</summary>
    public static IReadOnlyDictionary<CrawlStop, string> CrawlStops { get; } = new Dictionary<CrawlStop, string>
    {
        [CrawlStop.Exhausted] = "everything in scope was read: this is the whole of the site as far as it could be found",
        [CrawlStop.PageLimit] = "the page ceiling was reached and the site has more: this is a sample, not the site",
        [CrawlStop.TimeLimit] = "the time ran out and the site has more: this is a sample, not the site",
        [CrawlStop.DepthLimit] = "the depth ceiling was reached while following links: deeper pages were not read",
        [CrawlStop.Refused] = "the starting address could not be read at all, so nothing of the site was"
    };

    /// <summary>What is said of an address that neither answered nor failed in a way anybody recorded.</summary>
    public const string NoAnswer = "no answer came back for this address";

    /// <summary>What to do when a call over several pages read none of them.</summary>
    public const string NothingReadHint = "Each entry of failed says why and what that kind of failure calls for; if every one names the browser, call playwright_diagnose.";

    /// <summary>What is said when a call over several pages read none of them.</summary>
    public static string NothingRead(int asked) =>
        $"none of the {asked} pages could be read";

    /// <summary>Why a call with no addresses, or too many, is not run.</summary>
    public static string AddressesOutOfRange(int count) =>
        $"a call reads 1 to {Limits.MostAddresses} addresses, not {count}: read more in further calls";

    /// <summary>Why a per-page piece outside the range is refused rather than trimmed.</summary>
    public static string CharactersEachOutOfRange(int maxCharactersEach) =>
        $"maxCharactersEach is {Limits.FewestCharacters} to {Limits.MostCharactersEach}, not {maxCharactersEach}";

    /// <summary>Why a walk over more pages than one call may read is refused.</summary>
    public static string SitePagesOutOfRange(int maxPages) =>
        $"maxPages is 1 to {Limits.MostSitePages}, not {maxPages}";

    /// <summary>Why a walk deeper than one call may go is refused.</summary>
    public static string SiteDepthOutOfRange(int maxDepth) =>
        $"maxDepth is 0 to {Limits.MostSiteDepth}, not {maxDepth}";

    /// <summary>Why a walk shorter or longer than one call may take is refused.</summary>
    public static string SiteSecondsOutOfRange(int seconds) =>
        $"seconds is {Limits.FewestSiteSeconds} to {Limits.MostSiteSeconds}, not {seconds}";

    /// <summary>What is said when a valid expression matched nothing.</summary>
    public const string NothingSelected = "the expression is valid and matched nothing on this page: the page may name that part differently, or build it only after a move";

    /// <summary>Why an extraction that names its elements twice, or not at all, is not asked.</summary>
    public const string ExpressionUnnamed = "name the elements by css or by xpath, exactly one of the two";

    /// <summary>How an expression is written, for a caller whose expression would not parse.</summary>
    public const string ExpressionHint = "CSS as a browser takes it — table#prices tr, a.title, li > span:nth-child(2) — or XPath 1.0 over the rendered markup with lower-case names — //table[@id='prices']//tr, //a[contains(@href,'/docs/')].";

    /// <summary>How a field is written, for a caller that wrote one wrongly.</summary>
    public const string FieldHint = "A field is { name, css: [...], xpath: [...], attribute, many }: what to call the value, the selectors to try in order — CSS first, then XPath — the attribute to take instead of the text, and whether every match is wanted.";

    /// <summary>Why a record of no fields, or of too many, is refused.</summary>
    public static string FieldsOutOfRange(int count) =>
        $"fields is 1 to {Limits.MostFields}, not {count}";

    /// <summary>Why a field without a name cannot be answered.</summary>
    public const string FieldUnnamed = "every field needs a name: it is the key its value comes back under";

    /// <summary>Why two fields of one name cannot be answered.</summary>
    public static string FieldTwice(string name) =>
        $"two fields are named '{name}': each value comes back under its name, so names are unique";

    /// <summary>Why a field with no selector, or with too many, is refused.</summary>
    public static string CandidatesOutOfRange(string name, int count) =>
        $"field '{name}' tries {count} selectors, and a field tries 1 to {Limits.MostCandidates}";

    /// <summary>Why a field whose selector does not parse stops the whole record.</summary>
    public static string FieldInvalid(string name, Expression expression, string fault) =>
        $"field '{name}': {ExpressionInvalid(expression, fault)}";

    /// <summary>What is said of a record some of whose fields no selector found.</summary>
    public static string FieldsMissing(int missing, int fields) =>
        $"{missing} of {fields} fields matched no selector: the site may have been rebuilt — read the page with playwright_fetch and find them anew";

    /// <summary>How a move is written, for a caller that wrote one wrongly.</summary>
    public const string MoveHint = "A move is { action, css or label, value, expect, pattern, withinSeconds }: what to do, the element named one way, what to enter, and the reaction the site owes.";

    /// <summary>What to do about a move the page did not answer.</summary>
    public const string MoveFruitless = "A move produced nothing the walk could go on from. The error says what the page offered at that point — how many elements the address matched, which fields there are — so rewrite the move from that: the same move again gives the same result.";

    /// <summary>Why a move that names its element twice, or not at all, cannot be made.</summary>
    public const string ElementUnnamed = "name the element by css or by label, exactly one of the two";

    /// <summary>Why a reaction named without what to look for, or the other way round, cannot be waited for.</summary>
    public const string ExpectationHalf = "expect and pattern go together: the kind of reaction, and what to look for";

    /// <summary>Why an expression that does not parse is not asked.</summary>
    public static string ExpressionInvalid(Expression expression, string fault) =>
        $"'{expression.Text}' is not a valid {(expression.Kind == ExpressionKind.Css ? "CSS selector" : "XPath expression")}: {fault}";

    /// <summary>Why a number of pieces outside the range is refused rather than trimmed.</summary>
    public static string SelectedOutOfRange(int limit) =>
        $"limit is 1 to {Limits.MostSelected}, not {limit}";

    /// <summary>What a picture that stopped short leaves unseen.</summary>
    public static string PictureCut(bool wholePage) =>
        wholePage
            ? "the page is taller than a picture may be, and the picture stops there: the rest is read with playwright_fetch"
            : "the page runs on below the screen: ask for the whole page to see the rest";

    /// <summary>Why a walk with no moves, or too many, is not started.</summary>
    public static string MovesOutOfRange(int count) =>
        $"a walk makes 1 to {Limits.MostMoves} moves, not {count}";

    /// <summary>Which move is wrong, and how.</summary>
    public static string MoveInvalid(int position, string fault) =>
        $"move {position}: {fault}";

    /// <summary>Why a word that is not a move cannot be made.</summary>
    public static string ActionUnknown(string? action) =>
        $"'{action}' is not a move; the moves are {Spelled<StepKind>()}";

    /// <summary>Why a move that enters something needs something to enter.</summary>
    public static string ValueMissing(StepKind kind) =>
        $"{Spelled(kind)} needs a value: the text to type, or the text of the option to take";

    /// <summary>Why a word that is not a reaction cannot be waited for.</summary>
    public static string ExpectationUnknown(string expect) =>
        $"'{expect}' is not a reaction; the reactions are {Spelled<EffectKind>()}";

    /// <summary>Why a wait outside the range is refused rather than trimmed.</summary>
    public static string WithinOutOfRange(int seconds) =>
        $"withinSeconds is 1 to {Limits.MostWithinSeconds}, not {seconds}";

    /// <summary>What a walk that did not finish said, at the length the toolkit says it.</summary>
    /// <remarks>
    /// Not cut to one line the way a failed read is. The toolkit names the move that produced nothing and
    /// then describes the page it failed on — how many elements the address matched, what fields were
    /// offered instead — and that description is exactly what the next attempt is written from.
    /// </remarks>
    public static string Walked(PageFailure failure)
    {
        string told = failure.Message.Trim();

        return told.Length <= WalkedCharacters ? $"{failure.Kind}: {told}" : $"{failure.Kind}: {told[..WalkedCharacters].TrimEnd()}…";
    }

    /// <summary>What a walk that did not finish calls for.</summary>
    /// <remarks>
    /// The toolkit reports a move the site ignored as <see cref="FailureKind.Empty"/> — the page may well have
    /// content, but not the answer to this errand. Measured on 2026-10-04 on the first live walk: the advice
    /// for an empty PAGE («holds no text, retrying returns the same nothing») was handed to a caller whose
    /// selector had matched nothing, which is advice about a different failure.
    /// </remarks>
    public static string WalkHintFor(PageFailure failure) =>
        failure.Kind is FailureKind.Empty ? MoveFruitless : HintFor(failure);

    /// <summary>The names of a kind the way a caller writes them.</summary>
    public static string Spelled<TKind>()
        where TKind : struct, Enum =>
        string.Join(", ", Enum.GetValues<TKind>().Select(kind => Spelled(kind)));

    private static string Spelled<TKind>(TKind kind)
        where TKind : struct, Enum =>
        kind.ToString().ToLowerInvariant();

    private const int WalkedCharacters = 1500;

    /// <summary>What to do when the browser server does not answer.</summary>
    public static string BrowserUnreachable(string? server) =>
        $"No browser answers at {server ?? "the configured address"}. Start the {BrowserDefaults.Image} container, " +
        $"or point {ServerSettings.PrefixOf(ServerConfigExtensions.Name)}WEB__SCRAPING__BROWSER_SERVER at the one that is running. " +
        "Searching DuckDuckGo needs no browser; every other tool does.";

    /// <summary>What a failed page read calls for, the site's own request to be left alone included.</summary>
    public static string HintFor(PageFailure failure)
    {
        string advice = failure.Kind is FailureKind.Navigation ? ForStatus(failure.StatusCode) : PageHints[failure.Kind];

        return failure.RetryAfter is { } gap
            ? $"{advice} The site asked to be left alone for {(long)gap.TotalSeconds} seconds."
            : advice;
    }

    /// <summary>What a failed page read said, in one line.</summary>
    /// <remarks>
    /// The browser's own message carries a call log after its first line — which selector was awaited,
    /// until what — and that is a trace for whoever maintains the toolkit, not a reason a model can act on.
    /// </remarks>
    public static string Said(PageFailure failure)
    {
        string reason = failure.Message.Split('\n', 2)[0].Trim();

        return failure.StatusCode is { } status ? $"{failure.Kind} ({status}): {reason}" : $"{failure.Kind}: {reason}";
    }

    /// <summary>What a search nobody answered calls for: one line for each way the engines failed.</summary>
    public static string HintFor(IReadOnlyList<SearchAttempt> attempts) =>
        string.Join(' ', attempts
            .Where(attempt => attempt.Failure is not null)
            .Select(attempt => attempt.Failure!.Kind)
            .Distinct()
            .Select(kind => SearchHints[kind]));

    /// <summary>Why something that is not an absolute http address will not be read.</summary>
    public static string NotAnAddress(string? url) =>
        $"'{url}' is not an absolute http or https address. This tool takes the address of a page, never a query: find one with playwright_search.";

    /// <summary>Why an address inside the deployment's own network will not be read.</summary>
    public static string InwardAddress(string host) =>
        $"'{host}' is not on the public web: this server reads public pages only and does not open addresses of the network it runs in.";

    /// <summary>Why a search with no queries, or too many, is not run.</summary>
    public static string QueriesOutOfRange(int count) =>
        $"a search takes 1 to {Limits.MostQueries} queries, not {count}";

    /// <summary>Why an overlong query is not sent.</summary>
    public static string QueryTooLong(int length) =>
        $"a query runs to {Limits.MostQueryCharacters} characters at most, not {length}: a search engine reads a phrase, not a document";

    /// <summary>Why a number of links outside the range is refused rather than trimmed.</summary>
    public static string ResultsOutOfRange(int limit) =>
        $"limit is 1 to {Limits.MostResults}, not {limit}";

    /// <summary>Why a negative position is refused.</summary>
    public static string FromOutOfRange(int from) =>
        $"from is a position in the text, 0 or more, not {from}";

    /// <summary>Why a piece outside the size range is refused rather than trimmed.</summary>
    public static string CharactersOutOfRange(int maxCharacters) =>
        $"maxCharacters is {Limits.FewestCharacters} to {Limits.MostCharacters}, not {maxCharacters}";

    /// <summary>Why a position past the end of the text is refused.</summary>
    public static string FromBeyondTheEnd(int from, int total) =>
        $"from is {from}, and the text of this page is {total} characters long: there is nothing left to read";

    private static string ForStatus(int? status) => status switch
    {
        null => "The host could not be reached at all — a name that does not resolve, a refused connection or a failed handshake. Check the address against where it came from.",
        404 or 410 => "The site says this address does not exist. Check it against the search result it came from; retrying returns the same answer.",
        401 or 403 => "The site refuses this address to an anonymous visitor. Retrying does not change that: use another source.",
        429 or 503 => "The site is turning requests away for now. Come back later rather than at once.",
        >= 500 => "The site failed on its own side. One more attempt later is reasonable.",
        _ => PageHints[FailureKind.Navigation]
    };
}
