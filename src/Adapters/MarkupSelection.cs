using System.Xml.XPath;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Snail.Toolkit.Web.Text;

namespace Snail.MCP.Playwright.Adapters;

/// <summary>Selects by CSS or XPath over the markup the toolkit parsed, and reads each match the way the toolkit reads a page.</summary>
/// <remarks>
/// Both languages run over one parse of the markup, through the toolkit's own door, <see cref="Markup.Read"/>,
/// so the ceiling on nesting that protects the reader protects every selection too. Each match is turned into
/// text by the same extraction a whole page gets — a table row comes back as its cells a space apart, not as markup.
/// </remarks>
public sealed class MarkupSelection(ITextExtraction text) : IMarkupSelection
{
    private static readonly string[] Addresses = ["href", "src", "action"];

    /// <inheritdoc/>
    public Selection Take(string html, Expression expression, string? attribute, Uri address, int limit)
    {
        var root = Markup.Read(html).DocumentNode;

        try
        {
            IReadOnlyList<HtmlNode> matched = expression.Kind == ExpressionKind.Css
                ? [.. root.QuerySelectorAll(expression.Text)]
                : [.. root.SelectNodes(expression.Text) ?? Enumerable.Empty<HtmlNode>()];

            return new Selection([.. matched.Select(node => Said(node, attribute, address)).Where(piece => piece.Length > 0).Take(limit)], matched.Count);
        }
        catch (Exception refused) when (refused is XPathException or FormatException or InvalidOperationException or NotSupportedException)
        {
            return Selection.Invalid(refused.Message);
        }
    }

    private string Said(HtmlNode node, string? attribute, Uri address)
    {
        if (attribute is null)
            return Shortened(text.Clean(node.OuterHtml));

        string value = node.GetAttributeValue(attribute, string.Empty).Trim();

        return Addresses.Contains(attribute, StringComparer.OrdinalIgnoreCase) && Uri.TryCreate(address, value, out var absolute)
            ? absolute.AbsoluteUri
            : Shortened(value);
    }

    private static string Shortened(string piece) =>
        piece.Length <= Limits.SelectedCharacters ? piece : $"{piece[..Limits.SelectedCharacters].TrimEnd()}…";
}
