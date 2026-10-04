using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web.Search;

namespace Snail.MCP.Playwright.Application;

/// <summary>Answers «where is this written about»: queries in, links out, and never a page's content.</summary>
/// <remarks>
/// The split is the point. The moment a search hands back text, a model stops going to the pages and
/// answers from snippets — so a snippet is kept short enough to choose a link by and no longer.
/// </remarks>
public sealed class LinkSearch(IWebSearch search, ServerConfig config)
{
    /// <summary>The links the engines offer for these queries, and what every engine did to find them.</summary>
    public async Task<Answer> FindAsync(IReadOnlyList<string>? queries, int limit, CancellationToken cancellationToken = default)
    {
        var asked = Asked(queries);

        if (Refusal(asked, limit) is { } refusal)
            return Answer.Failed(refusal, Messages.SearchHint);

        var outcome = await search.FindAsync(asked, TimeSpan.FromSeconds(config.SearchSeconds), cancellationToken);

        return outcome.IsAnswered
            ? Answer.Ok(Described(outcome, limit))
            : Answer.Stopped(Described(outcome, limit), Messages.NobodyAnswered, Messages.HintFor(outcome.Attempts));
    }

    private static string? Refusal(IReadOnlyList<string> asked, int limit)
    {
        if (asked.Count is 0 or > Limits.MostQueries)
            return Messages.QueriesOutOfRange(asked.Count);

        if (asked.FirstOrDefault(query => query.Length > Limits.MostQueryCharacters) is { } overlong)
            return Messages.QueryTooLong(overlong.Length);

        return limit is < 1 or > Limits.MostResults ? Messages.ResultsOutOfRange(limit) : null;
    }

    private static IReadOnlyList<string> Asked(IReadOnlyList<string>? queries) =>
        [.. (queries ?? []).Where(query => !string.IsNullOrWhiteSpace(query)).Select(query => query.Trim()).Distinct(StringComparer.Ordinal)];

    private static JsonObject Described(SearchOutcome outcome, int limit) => new()
    {
        ["results"] = new JsonArray([.. outcome.Results.Take(limit).Select(Described)]),
        ["found"] = outcome.Results.Count,
        ["note"] = outcome.IsAnswered && !outcome.HasResults ? Messages.NothingMatched : null,
        ["engines"] = new JsonArray([.. outcome.Attempts.Select(Described)])
    };

    private static JsonNode Described(SearchResult result) => new JsonObject
    {
        ["title"] = result.Title,
        ["url"] = result.Address.Text,
        ["snippet"] = Shortened(result.Snippet),
        ["engine"] = result.Provider
    };

    private static JsonNode Described(SearchAttempt attempt) => new JsonObject
    {
        ["engine"] = attempt.Provider,
        ["query"] = attempt.Query,
        ["results"] = attempt.Results.Count,
        ["milliseconds"] = (long)attempt.Duration.TotalMilliseconds,
        ["failure"] = attempt.Failure?.Kind.ToString(),
        ["message"] = attempt.Failure?.Message,
        ["opensAt"] = attempt.Failure?.OpensAt?.ToString("O")
    };

    private static string Shortened(string snippet) =>
        snippet.Length <= Limits.SnippetCharacters ? snippet : $"{snippet[..Limits.SnippetCharacters].TrimEnd()}…";
}
