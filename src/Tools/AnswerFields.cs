using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web.Scraping;
using Snail.Toolkit.Web.Search;

namespace Snail.MCP.Playwright.Tools;

/// <summary>What the tools answer with, as the data of the envelope <see cref="ToolAnswer"/> writes.</summary>
public static class AnswerFields
{
    /// <summary>A search as an answer carries it: the links offered, how many were found, and what every engine did.</summary>
    public static JsonObject Of(LinksFound found) => new()
    {
        ["results"] = new JsonArray([.. found.Offered.Select(Of)]),
        ["found"] = found.Found,
        ["note"] = found.IsNothingMatched ? Messages.NothingMatched : null,
        ["engines"] = new JsonArray([.. found.Attempts.Select(Of)])
    };

    /// <summary>A link as a search offers it: a snippet short enough to choose by and no longer.</summary>
    public static JsonObject Of(SearchResult result) => new()
    {
        ["title"] = result.Title,
        ["url"] = result.Address.Text,
        ["snippet"] = Shortened(result.Snippet),
        ["engine"] = result.Provider
    };

    /// <summary>One engine's attempt at one query: what it found, how long it took, and how it failed.</summary>
    public static JsonObject Of(SearchAttempt attempt) => new()
    {
        ["engine"] = attempt.Provider,
        ["query"] = attempt.Query,
        ["results"] = attempt.Results.Count,
        ["milliseconds"] = (long)attempt.Duration.TotalMilliseconds,
        ["failure"] = attempt.Failure?.Kind.ToString(),
        ["message"] = attempt.Failure?.Message,
        ["opensAt"] = attempt.Failure?.OpensAt is { } opensAt ? ToolAnswer.Moment(opensAt) : null
    };

    /// <summary>A piece of a page as a caller reads it: where the page is, how much of it this is, and the text.</summary>
    public static JsonObject Of(PagePiece piece) => new()
    {
        ["url"] = piece.Page.Final.Text,
        ["requested"] = piece.Page.IsRedirected ? piece.Page.Requested.Text : null,
        ["title"] = piece.Page.Title,
        ["status"] = piece.Page.StatusCode,
        ["fetchedAt"] = ToolAnswer.Moment(piece.Page.FetchedAt),
        ["characters"] = new JsonObject { ["from"] = piece.From, ["returned"] = piece.Text.Length, ["total"] = piece.Page.Text.Length },
        ["next"] = piece.Next,
        ["text"] = piece.Text
    };

    /// <summary>A page that failed, as the failure of a read carries it: what happened and whether trying again is worth it.</summary>
    public static JsonObject Of(PageFailure failure) => new()
    {
        ["url"] = failure.Address?.Text,
        ["kind"] = failure.Kind.ToString(),
        ["status"] = failure.StatusCode,
        ["retryAfterSeconds"] = failure.RetryAfter is { } gap ? (long)gap.TotalSeconds : null,
        ["isWorthRetrying"] = failure.IsTransient
    };

    /// <summary>A batch as an answer carries it: how many were asked and read, the pieces, and why each missing page is missing.</summary>
    public static JsonObject Of(PagesRead read) => new()
    {
        ["asked"] = read.Asked,
        ["read"] = read.Pages.Count,
        ["pages"] = new JsonArray([.. read.Pages.Select(Of)]),
        ["failed"] = new JsonArray([.. read.Unread.Select(Of)])
    };

    /// <summary>An address of a batch that was not read, and what that calls for.</summary>
    public static JsonObject Of(UnreadPage unread) => new()
    {
        ["url"] = unread.Address.Text,
        ["kind"] = unread.Failure?.Kind.ToString(),
        ["status"] = unread.Failure?.StatusCode,
        ["error"] = unread.Error,
        ["hint"] = unread.Failure is { } failure ? Messages.HintFor(failure) : null
    };

    /// <summary>A walk of a site as an answer carries it: why it ended, the pages read and the pages that failed.</summary>
    public static JsonObject Of(SiteRead site) => new()
    {
        ["start"] = site.Report.Start.Text,
        ["stop"] = site.Report.Stop.ToString(),
        ["stopMeans"] = Messages.CrawlStops[site.Report.Stop],
        ["isFromSitemap"] = site.Report.IsFromSitemap,
        ["seconds"] = (long)site.Report.Elapsed.TotalSeconds,
        ["read"] = site.Report.Read,
        ["pages"] = new JsonArray([.. site.Pages.Select(Of)]),
        ["failed"] = new JsonArray([.. site.Failed.Select(Unreached)])
    };

    /// <summary>A page of a walked site: where it was found and the part of its text an entry carries.</summary>
    public static JsonObject Of(SitePage page) => new()
    {
        ["url"] = page.Page.Address.Text,
        ["title"] = page.Page.Title,
        ["depth"] = page.Page.Depth,
        ["characters"] = new JsonObject { ["returned"] = page.Text.Length, ["total"] = page.Page.OriginalLength },
        ["text"] = page.Text
    };

    /// <summary>What an expression selected in a page, with a note when it matched nothing.</summary>
    public static JsonObject Of(PiecesSelected selected) => new()
    {
        ["url"] = selected.Page.Final.Text,
        ["title"] = selected.Page.Title,
        ["matched"] = selected.Selected.Matched,
        ["returned"] = selected.Selected.Pieces.Count,
        ["note"] = selected.Selected.Matched == 0 ? Messages.NothingSelected : null,
        ["pieces"] = Of(selected.Selected.Pieces)
    };

    /// <summary>A record taken out of a page: the values by name, the selector that found each, and the fields found by none.</summary>
    public static JsonObject Of(FieldsTaken taken)
    {
        var missing = taken.Fields.Where(field => !field.IsFound).Select(field => field.Name).ToList();

        return new JsonObject
        {
            ["url"] = taken.Page.Final.Text,
            ["title"] = taken.Page.Title,
            ["fields"] = new JsonObject([.. taken.Fields.Select(field => KeyValuePair.Create(field.Name, ValueOf(field)))]),
            ["found"] = new JsonArray([.. taken.Fields.Where(field => field.IsFound).Select(Of)]),
            ["missing"] = missing.Count == 0 ? null : Of(missing),
            ["note"] = missing.Count == 0 ? null : Messages.FieldsMissing(missing.Count, taken.Fields.Count)
        };
    }

    /// <summary>How a field was found: its name, the selector that matched under its kind, and how many elements it matched.</summary>
    public static JsonObject Of(FieldTaken field) => new()
    {
        ["name"] = field.Name,
        [field.Expression?.Kind == ExpressionKind.Css ? "css" : "xpath"] = field.Expression?.Text,
        ["matched"] = field.Matched
    };

    /// <summary>The words that go before a picture: what it shows, where it came from and whether it stops short of the page.</summary>
    public static JsonObject Of(PagePicture picture) => new()
    {
        ["url"] = picture.Snapshot.Final.Text,
        ["requested"] = picture.Snapshot.Final != picture.Snapshot.Requested ? picture.Snapshot.Requested.Text : null,
        ["title"] = picture.Snapshot.Title,
        ["status"] = picture.Snapshot.StatusCode,
        ["takenAt"] = ToolAnswer.Moment(picture.Snapshot.TakenAt),
        ["width"] = picture.Snapshot.Width,
        ["height"] = picture.Snapshot.Height,
        ["bytes"] = picture.Snapshot.Image.Length,
        ["isCut"] = picture.Snapshot.IsCut,
        ["note"] = picture.Snapshot.IsCut ? Messages.PictureCut(picture.IsWholePage) : null
    };

    /// <summary>The page the moves led to: where it is and as much of its text as the answer carries.</summary>
    public static JsonObject Of(PageWalked walked) => new()
    {
        ["url"] = walked.Page.Final.Text,
        ["title"] = walked.Page.Title,
        ["status"] = walked.Page.StatusCode,
        ["characters"] = new JsonObject { ["returned"] = walked.Text.Length, ["total"] = walked.Page.Text.Length },
        ["isCut"] = walked.Text.Length < walked.Page.Text.Length,
        ["text"] = walked.Text
    };

    /// <summary>The page a walk stopped on and how.</summary>
    public static JsonObject Of(WalkStopped stopped) => new()
    {
        ["url"] = stopped.Failure.Address?.Text,
        ["kind"] = stopped.Failure.Kind.ToString(),
        ["status"] = stopped.Failure.StatusCode
    };

    /// <summary>The state of the server, of the browser behind it and of the search it offers.</summary>
    public static JsonObject Of(HealthReport report) => new()
    {
        ["server"] = new JsonObject
        {
            ["name"] = report.Name,
            ["version"] = report.Version,
            ["transport"] = ToolAnswer.Word(report.Transport)
        },
        ["browser"] = Of(report.Browser),
        ["search"] = new JsonObject { ["engines"] = Of(report.Engines) }
    };

    /// <summary>Whether the browser answered, and what to start when it did not.</summary>
    public static JsonObject Of(BrowserState browser) => new()
    {
        ["server"] = browser.Server,
        ["engine"] = browser.Engine,
        ["isReachable"] = browser.IsReachable,
        ["version"] = browser.Version,
        ["milliseconds"] = (long)browser.Took.TotalMilliseconds,
        ["error"] = browser.Error,
        ["hint"] = browser.IsReachable ? null : Messages.BrowserUnreachable(browser.Server)
    };

    private static JsonArray Of(IEnumerable<string> words) => new([.. words.Select(word => JsonValue.Create(word))]);

    private static JsonNode? ValueOf(FieldTaken field)
    {
        if (!field.IsFound)
            return null;

        return field.IsEveryMatchWanted ? Of(field.Pieces) : JsonValue.Create(field.Pieces[0]);
    }

    private static JsonObject Unreached(PageFailure failure) => new()
    {
        ["url"] = failure.Address?.Text,
        ["kind"] = failure.Kind.ToString(),
        ["status"] = failure.StatusCode,
        ["error"] = Messages.Said(failure)
    };

    private static string Shortened(string snippet) =>
        snippet.Length <= Limits.SnippetCharacters ? snippet : $"{snippet[..Limits.SnippetCharacters].TrimEnd()}…";
}
