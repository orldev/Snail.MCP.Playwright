using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web;
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
    public async Task<Answer<PageWalked>> PerformAsync(string url, IReadOnlyList<Move>? moves, int maxCharacters, CancellationToken cancellationToken = default)
    {
        var address = Link.Of(url ?? string.Empty);

        if (Refusal(address, moves, maxCharacters) is { } refusal)
            return refusal with { Hint = Messages.MoveHint };

        var errand = new Errand(address, [.. moves!.Select(Stepped)]);
        var outcome = await agent.PerformAsync(errand, cancellationToken);

        return outcome.TryGetValue(out var page)
            ? Answer.Ok(new PageWalked(page, Piece.Cut(page.Text, 0, maxCharacters)))
            : Answer.Failed(Messages.Walked(outcome.Failure), Messages.WalkHintFor(outcome.Failure), FailureTypes.Of(outcome.Failure.Kind)).WithDetails(new WalkStopped(outcome.Failure));
    }

    private AnswerFailure? Refusal(Link address, IReadOnlyList<Move>? moves, int maxCharacters)
    {
        if (rules.Refusal(address) is { } inadmissible)
            return inadmissible;

        if (moves is null or { Count: 0 or > Limits.MostMoves })
            return FailureTypes.Refused(Messages.MovesOutOfRange(moves?.Count ?? 0));

        if (maxCharacters is < Limits.FewestCharacters or > Limits.MostCharacters)
            return FailureTypes.Refused(Messages.CharactersOutOfRange(maxCharacters));

        return FirstFault(moves) is { } fault ? FailureTypes.Refused(fault) : null;
    }

    private static string? FirstFault(IReadOnlyList<Move> moves)
    {
        for (int position = 0; position < moves.Count; position++)
        {
            if (Fault(moves[position]) is { } fault)
                return Messages.MoveInvalid(position + 1, fault);
        }

        return null;
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

        var where = move.Label is { Length: > 0 } label ? Spot.Labelled(label) : Spot.Css(move.Css!);
        var step = new Step(kind, where)
        {
            Value = move.Value,
            Expected = Expected(move)
        };

        return step;
    }

    private static Effect? Expected(Move move) =>
        TryReadKind<EffectKind>(move.Expect, out var kind)
            ? new Effect(kind, move.Pattern!) { Within = TimeSpan.FromSeconds(move.WithinSeconds ?? Limits.DefaultWithinSeconds) }
            : null;

    /// <summary>Reads a kind by the one word that names it.</summary>
    /// <remarks>
    /// Not <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>: it reads a number as a value and a
    /// comma-separated list as the values ORed together, for any enum, so <c>pick,click</c> parsed as
    /// <see cref="StepKind.Press"/> and passed the validation — found by the audit of 2026-10-16.
    /// </remarks>
    private static bool TryReadKind<TKind>(string? name, out TKind kind)
        where TKind : struct, Enum
    {
        kind = default;

        if (string.IsNullOrWhiteSpace(name))
            return false;

        foreach (var candidate in Enum.GetValues<TKind>())
        {
            if (!candidate.ToString().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            kind = candidate;

            return true;
        }

        return false;
    }
}
