using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>Reads several known addresses at once: one call, one answer, a failure beside the pages rather than instead of them.</summary>
/// <remarks>
/// One unreachable site must not cost the caller the nine that answered, so a page that failed is an
/// entry in the answer and the call fails only when nothing at all was read.
/// </remarks>
public sealed class BatchReading(ICrawler crawler, AddressRules rules, PageMemory memory)
{
    /// <summary>A piece of every page that answered, and the reason for every one that did not.</summary>
    public async Task<Answer<JsonNode>> ReadAsync(IReadOnlyList<string>? urls, int maxCharactersEach, CancellationToken cancellationToken = default)
    {
        var asked = Asked(urls);

        if (Refusal(asked, maxCharactersEach) is { } refusal)
            return refusal;

        var read = await ReadAsync([.. asked.Where(address => rules.Refusal(address) is null)], cancellationToken);
        var answered = Described(asked, read, maxCharactersEach);

        return read.Values.Any(outcome => outcome.IsSuccess)
            ? Answer.Ok<JsonNode>(answered)
            : Answer.Failed(Messages.NothingRead(asked.Count), Messages.NothingReadHint, FailureTypes.NothingRead, answered);
    }

    private async Task<Dictionary<Link, Outcome<PageContent, PageFailure>>> ReadAsync(IReadOnlyList<Link> admitted, CancellationToken cancellationToken)
    {
        var read = admitted
            .Select(address => memory.Of(address))
            .OfType<PageContent>()
            .ToDictionary(page => page.Requested, Outcome<PageContent, PageFailure>.Success);

        foreach (var outcome in await crawler.ReadAllAsync(admitted.Where(address => !read.ContainsKey(address)), cancellationToken))
        {
            if (outcome.TryGetValue(out var page))
                memory.Keep(page);

            if ((page?.Requested ?? outcome.Failure?.Address) is { } address)
                read[address] = outcome;
        }

        return read;
    }

    private JsonObject Described(IReadOnlyList<Link> asked, Dictionary<Link, Outcome<PageContent, PageFailure>> read, int maxCharactersEach)
    {
        var pages = new JsonArray();
        var failed = new JsonArray();

        foreach (var address in asked)
        {
            if (read.TryGetValue(address, out var outcome) && outcome.TryGetValue(out var page))
                pages.Add(Piece.Described(page, 0, maxCharactersEach));
            else
                failed.Add(Failed(address, outcome?.Failure));
        }

        return new JsonObject { ["asked"] = asked.Count, ["read"] = pages.Count, ["pages"] = pages, ["failed"] = failed };
    }

    private JsonObject Failed(Link address, PageFailure? failure) => new()
    {
        ["url"] = address.Text,
        ["kind"] = failure?.Kind.ToString(),
        ["status"] = failure?.StatusCode,
        ["error"] = failure is null ? rules.Refusal(address)?.Error ?? Messages.NoAnswer : Messages.Said(failure),
        ["hint"] = failure is null ? null : Messages.HintFor(failure)
    };

    private static AnswerFailure? Refusal(IReadOnlyList<Link> asked, int maxCharactersEach)
    {
        if (asked.Count is 0 or > Limits.MostAddresses)
            return FailureTypes.Refused(Messages.AddressesOutOfRange(asked.Count));

        return maxCharactersEach is < Limits.FewestCharacters or > Limits.MostCharactersEach
            ? FailureTypes.Refused(Messages.CharactersEachOutOfRange(maxCharactersEach))
            : null;
    }

    private static IReadOnlyList<Link> Asked(IReadOnlyList<string>? urls) =>
        [.. (urls ?? []).Where(url => !string.IsNullOrWhiteSpace(url)).Select(Link.Of).Distinct()];
}
