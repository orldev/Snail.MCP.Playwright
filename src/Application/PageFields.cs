using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web;

namespace Snail.MCP.Playwright.Application;

/// <summary>Takes a record out of a page: named values, each found by the first of its selectors that matches.</summary>
/// <remarks>
/// What a collection that runs every month needs. The selectors that worked last time are passed first and a
/// broader one behind them; the answer says which one matched each field and which fields were found by none,
/// so a site that was rebuilt shows up as named fields gone missing rather than as a record quietly half empty.
/// </remarks>
public sealed class PageFields(PageReading pages, AddressRules rules, IMarkupSelection selection)
{
    /// <summary>The values of the fields in the page, and how each was found.</summary>
    public async Task<Answer<JsonNode>> TakeAsync(string url, IReadOnlyList<Field>? fields, CancellationToken cancellationToken = default)
    {
        var address = Link.Of(url ?? string.Empty);

        if (Refusal(address, fields) is { } refusal)
            return Answer.Failed(refusal, Messages.FieldHint);

        var outcome = await pages.PageAsync(address, cancellationToken);

        if (!outcome.TryGetValue(out var page))
            return PageReading.Refused(outcome.Failure!);

        Uri origin = page.Final.Address ?? address.Address!;
        var values = new JsonObject();
        var found = new JsonArray();
        List<string> missing = [];

        foreach (var field in fields!)
        {
            var taken = Take(page.Html, field, origin);

            if (taken.Fault is { } fault)
                return Answer.Failed(Messages.FieldInvalid(field.Name, taken.Expression!, fault), Messages.ExpressionHint);

            if (taken.Expression is null)
            {
                missing.Add(field.Name);
                values[field.Name] = null;
                continue;
            }

            values[field.Name] = field.IsEveryMatchWanted ? new JsonArray([.. taken.Pieces.Select(piece => (JsonNode)piece)]) : taken.Pieces[0];
            found.Add(new JsonObject
            {
                ["name"] = field.Name,
                [taken.Expression.Kind == ExpressionKind.Css ? "css" : "xpath"] = taken.Expression.Text,
                ["matched"] = taken.Matched
            });
        }

        return Answer.Ok<JsonNode>(new JsonObject
        {
            ["url"] = page.Final.Text,
            ["title"] = page.Title,
            ["fields"] = values,
            ["found"] = found,
            ["missing"] = missing.Count == 0 ? null : new JsonArray([.. missing.Select(name => (JsonNode)name)]),
            ["note"] = missing.Count == 0 ? null : Messages.FieldsMissing(missing.Count, fields!.Count)
        });
    }

    private Taken Take(string html, Field field, Uri origin)
    {
        foreach (var candidate in field.Candidates)
        {
            var selected = selection.Take(html, candidate, PageExtraction.Named(field.Attribute), origin, field.IsEveryMatchWanted ? Limits.MostSelected : 1);

            if (!selected.IsValid)
                return new Taken(candidate, [], 0, selected.Fault);

            if (selected.Pieces.Count > 0)
                return new Taken(candidate, selected.Pieces, selected.Matched, null);
        }

        return new Taken(null, [], 0, null);
    }

    private string? Refusal(Link address, IReadOnlyList<Field>? fields)
    {
        if (rules.Refusal(address) is { } inadmissible)
            return inadmissible;

        if (fields is null || fields.Count is < 1 or > Limits.MostFields)
            return Messages.FieldsOutOfRange(fields?.Count ?? 0);

        if (fields.FirstOrDefault(field => string.IsNullOrWhiteSpace(field.Name)) is not null)
            return Messages.FieldUnnamed;

        if (fields.GroupBy(field => field.Name.Trim(), StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1) is { } twice)
            return Messages.FieldTwice(twice.Key);

        if (fields.FirstOrDefault(field => field.Candidates.Count is < 1 or > Limits.MostCandidates) is { } wrong)
            return Messages.CandidatesOutOfRange(wrong.Name, wrong.Candidates.Count);

        return null;
    }

    private sealed record Taken(Expression? Expression, IReadOnlyList<string> Pieces, int Matched, string? Fault);
}
