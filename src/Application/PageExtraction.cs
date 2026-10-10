using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web;

namespace Snail.MCP.Playwright.Application;

/// <summary>Takes named pieces out of a page instead of its whole text: the rows of a table, the links of a list.</summary>
/// <remarks>
/// The page is the one the reader read. A caller that fetched a page, saw a table in its text and now wants the
/// rows asks about the same markup, and the site is not visited again for it.
/// </remarks>
public sealed class PageExtraction(PageReading pages, AddressRules rules, IMarkupSelection selection)
{
    /// <summary>What the expression matches in the page: the text of each element, or one attribute of each.</summary>
    public async Task<Answer> SelectAsync(string url, string? xpath, string? css, string? attribute, int limit, CancellationToken cancellationToken = default)
    {
        var address = Link.Of(url ?? string.Empty);

        if (Refusal(address, xpath, css, limit) is { } refusal)
            return Answer.Failed(refusal, Messages.ExpressionHint);

        var outcome = await pages.PageAsync(address, cancellationToken);

        if (!outcome.TryGetValue(out var page))
            return PageReading.Refused(outcome.Failure!);

        var expression = string.IsNullOrWhiteSpace(css) ? Expression.XPath(xpath!.Trim()) : Expression.Css(css.Trim());
        var selected = selection.Take(page.Html, expression, Named(attribute), page.Final.Address ?? address.Address!, limit);

        return selected.IsValid
            ? Answer.Ok(new JsonObject
            {
                ["url"] = page.Final.Text,
                ["title"] = page.Title,
                ["matched"] = selected.Matched,
                ["returned"] = selected.Pieces.Count,
                ["note"] = selected.Matched == 0 ? Messages.NothingSelected : null,
                ["pieces"] = new JsonArray([.. selected.Pieces.Select(piece => (JsonNode)piece)])
            })
            : Answer.Failed(Messages.ExpressionInvalid(expression, selected.Fault!), Messages.ExpressionHint);
    }

    /// <summary>The attribute as the markup names it: without the at sign XPath writes before it, blank as none.</summary>
    internal static string? Named(string? attribute) =>
        string.IsNullOrWhiteSpace(attribute) ? null : attribute.Trim().TrimStart('@');

    private string? Refusal(Link address, string? xpath, string? css, int limit)
    {
        if (rules.Refusal(address) is { } inadmissible)
            return inadmissible;

        if (string.IsNullOrWhiteSpace(xpath) == string.IsNullOrWhiteSpace(css))
            return Messages.ExpressionUnnamed;

        return limit is < 1 or > Limits.MostSelected ? Messages.SelectedOutOfRange(limit) : null;
    }
}
