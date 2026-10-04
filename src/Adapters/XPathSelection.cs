using System.Xml.XPath;
using HtmlAgilityPack;
using Snail.Toolkit.Web.Text;

namespace Snail.MCP.Playwright.Adapters;

/// <summary>Selects by XPath over the markup the toolkit parsed, and reads each match the way the toolkit reads a page.</summary>
/// <remarks>
/// The markup goes through the toolkit's own door, <see cref="Markup.Read"/>, so the ceiling on nesting
/// that protects the reader protects this too. Each match is turned into text by the same extraction a
/// whole page gets — a table row comes back as its cells a space apart, not as markup.
/// </remarks>
public sealed class XPathSelection(ITextExtraction text) : IMarkupSelection
{
    private static readonly string[] Addresses = ["href", "src", "action"];

    /// <inheritdoc/>
    public Selection Take(string html, string xpath, string? attribute, Uri address, int limit)
    {
        try
        {
            HtmlNodeCollection? matched = Markup.Read(html).DocumentNode.SelectNodes(xpath);

            return matched is null
                ? new Selection([], 0)
                : new Selection([.. matched.Select(node => Said(node, attribute, address)).Where(piece => piece.Length > 0).Take(limit)], matched.Count);
        }
        catch (XPathException refused)
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
