using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>Answers «what does this page say»: an address in, its text out, a piece at a time.</summary>
public sealed class PageReading(IPageReader reader, AddressRules rules, PageMemory memory)
{
    /// <summary>The text of the page from <paramref name="from"/> on, as much of it as one answer may carry.</summary>
    public async Task<Answer<JsonNode>> ReadAsync(string url, int from, int maxCharacters, CancellationToken cancellationToken = default)
    {
        var address = Link.Of(url ?? string.Empty);

        if (Refusal(address, from, maxCharacters) is { } refusal)
            return refusal;

        var outcome = await PageAsync(address, cancellationToken);

        return outcome.TryGetValue(out var page)
            ? Piece.Of(page, from, maxCharacters)
            : Refused(outcome.Failure!);
    }

    /// <summary>The page itself, from memory when it was read a moment ago and from the site otherwise.</summary>
    public async Task<Outcome<PageContent, PageFailure>> PageAsync(Link address, CancellationToken cancellationToken = default)
    {
        if (memory.Of(address) is { } remembered)
            return Outcome<PageContent, PageFailure>.Success(remembered);

        var outcome = await reader.ReadAsync(address, cancellationToken);

        if (outcome.TryGetValue(out var page))
            memory.Keep(page);

        return outcome;
    }

    /// <summary>A failed read as an answer: what happened, and what it calls for.</summary>
    public static Answer<JsonNode> Refused(PageFailure failure) =>
        Answer.Failed(Messages.Said(failure), Messages.HintFor(failure), FailureTypes.Of(failure.Kind), Described(failure));

    private AnswerFailure? Refusal(Link address, int from, int maxCharacters)
    {
        if (rules.Refusal(address) is { } inadmissible)
            return inadmissible;

        if (from < 0)
            return FailureTypes.Refused(Messages.FromOutOfRange(from));

        return maxCharacters is < Limits.FewestCharacters or > Limits.MostCharacters
            ? FailureTypes.Refused(Messages.CharactersOutOfRange(maxCharacters))
            : null;
    }

    private static JsonObject Described(PageFailure failure) => new()
    {
        ["url"] = failure.Address?.Text,
        ["kind"] = failure.Kind.ToString(),
        ["status"] = failure.StatusCode,
        ["retryAfterSeconds"] = failure.RetryAfter is { } gap ? (long)gap.TotalSeconds : null,
        ["isWorthRetrying"] = failure.IsTransient
    };
}
