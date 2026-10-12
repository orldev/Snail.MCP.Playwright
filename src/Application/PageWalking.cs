using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;
using Snail.Toolkit.Web.Scraping.Agent;

namespace Snail.MCP.Playwright.Application;

/// <summary>Uses a page instead of only reading it: opens it, makes a few moves, and reads what resulted.</summary>
/// <remarks>
/// For what a page says only once a form is filled in — a registry search, a filter, a site's own
/// search box. The page that results is an answer to those moves and to nothing else, so it is never
/// remembered under its address: the next plain read of that address must not be handed it.
/// </remarks>
public sealed class PageWalking(IPageAgent agent, AddressRules rules)
{
    /// <summary>The text of the page after the moves were made, or the move the site did not answer.</summary>
    public async Task<Answer> PerformAsync(string url, IReadOnlyList<Move>? moves, int maxCharacters, CancellationToken cancellationToken = default)
    {
        var address = Link.Of(url ?? string.Empty);

        if (Refusal(address, moves, maxCharacters) is { } refusal)
            return Answer.Failed(refusal, Messages.MoveHint);

        var outcome = await agent.PerformAsync(new Errand(address, [.. moves!.Select(Stepped)]), cancellationToken);

        return outcome.TryGetValue(out var page)
            ? Answer.Ok(Described(page, maxCharacters))
            : Answer.Stopped(Described(outcome.Failure!), Messages.Walked(outcome.Failure!), Messages.WalkHintFor(outcome.Failure!));
    }

    private string? Refusal(Link address, IReadOnlyList<Move>? moves, int maxCharacters)
    {
        if (rules.Refusal(address) is { } inadmissible)
            return inadmissible;

        if (moves is null or { Count: 0 or > Limits.MostMoves })
            return Messages.MovesOutOfRange(moves?.Count ?? 0);

        if (maxCharacters is < Limits.FewestCharacters or > Limits.MostCharacters)
            return Messages.CharactersOutOfRange(maxCharacters);

        return moves.Select((move, index) => Fault(move) is { } fault ? Messages.MoveInvalid(index + 1, fault) : null).FirstOrDefault(fault => fault is not null);
    }

    /// <summary>What is wrong with a move as written, or null when it can be made.</summary>
    internal static string? Fault(Move move)
    {
        if (!TryReadKind<StepKind>(move.Action, out var kind))
            return Messages.ActionUnknown(move.Action);

        if (string.IsNullOrWhiteSpace(move.Css) == string.IsNullOrWhiteSpace(move.Label))
            return Messages.ElementUnnamed;

        if (kind is StepKind.Type or StepKind.Pick or StepKind.Choose && string.IsNullOrEmpty(move.Value))
            return Messages.ValueMissing(kind);

        if (string.IsNullOrWhiteSpace(move.Expect) != string.IsNullOrWhiteSpace(move.Pattern))
            return Messages.ExpectationHalf;

        if (move.Expect is not null && !TryReadKind<EffectKind>(move.Expect, out _))
            return Messages.ExpectationUnknown(move.Expect);

        return move.WithinSeconds is < 1 or > Limits.MostWithinSeconds ? Messages.WithinOutOfRange(move.WithinSeconds.Value) : null;
    }

    private static Step Stepped(Move move)
    {
        TryReadKind<StepKind>(move.Action, out var kind);

        return new Step(kind, move.Label is { Length: > 0 } label ? Spot.Labelled(label) : Spot.Css(move.Css!))
        {
            Value = move.Value,
            Expected = Expected(move)
        };
    }

    private static Effect? Expected(Move move) =>
        TryReadKind<EffectKind>(move.Expect, out var kind)
            ? new Effect(kind, move.Pattern!) { Within = TimeSpan.FromSeconds(move.WithinSeconds ?? Limits.DefaultWithinSeconds) }
            : null;

    /// <summary>Reads a kind by its name alone: a number would parse as an enum and name nothing a caller meant.</summary>
    private static bool TryReadKind<TKind>(string? name, out TKind kind)
        where TKind : struct, Enum
    {
        kind = default;

        return !string.IsNullOrWhiteSpace(name)
               && !int.TryParse(name, out _)
               && Enum.TryParse(name.Trim(), ignoreCase: true, out kind)
               && Enum.IsDefined(kind);
    }

    private static JsonObject Described(PageContent page, int maxCharacters)
    {
        string text = Piece.Cut(page.Text, 0, maxCharacters);

        return new JsonObject
        {
            ["url"] = page.Final.Text,
            ["title"] = page.Title,
            ["status"] = page.StatusCode,
            ["characters"] = new JsonObject { ["returned"] = text.Length, ["total"] = page.Text.Length },
            ["isCut"] = text.Length < page.Text.Length,
            ["text"] = text
        };
    }

    private static JsonObject Described(PageFailure failure) => new()
    {
        ["url"] = failure.Address?.Text,
        ["kind"] = failure.Kind.ToString(),
        ["status"] = failure.StatusCode
    };
}
