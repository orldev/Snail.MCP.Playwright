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
    public async Task<Answer<LinksFound>> FindAsync(IReadOnlyList<string>? queries, int limit, CancellationToken cancellationToken = default)
    {
        var asked = Asked(queries);

        if (Refusal(asked, limit) is { } refusal)
            return refusal with { Hint = Messages.SearchHint };

        var outcome = await search.FindAsync(asked, TimeSpan.FromSeconds(config.SearchSeconds), cancellationToken);
        var found = new LinksFound([.. outcome.Results.Take(limit)], outcome.Results.Count, outcome.IsAnswered && !outcome.HasResults, outcome.Attempts);

        return outcome.IsAnswered
            ? Answer.Ok(found)
            : Answer.Failed(Messages.NobodyAnswered, Messages.HintFor(outcome.Attempts), FailureTypes.Unanswered).WithDetails(found);
    }

    private static AnswerFailure? Refusal(IReadOnlyList<string> asked, int limit)
    {
        if (asked.Count is 0 or > Limits.MostQueries)
            return FailureTypes.Refused(Messages.QueriesOutOfRange(asked.Count));

        if (asked.FirstOrDefault(query => query.Length > Limits.MostQueryCharacters) is { } overlong)
            return FailureTypes.Refused(Messages.QueryTooLong(overlong.Length));

        return limit is < 1 or > Limits.MostResults ? FailureTypes.Refused(Messages.ResultsOutOfRange(limit)) : null;
    }

    private static IReadOnlyList<string> Asked(IReadOnlyList<string>? queries) =>
        [.. (queries ?? []).Where(query => !string.IsNullOrWhiteSpace(query)).Select(query => query.Trim()).Distinct(StringComparer.Ordinal)];
}
