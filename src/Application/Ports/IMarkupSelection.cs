namespace Snail.MCP.Playwright.Application.Ports;

/// <summary>Takes named pieces out of a page's markup.</summary>
public interface IMarkupSelection
{
    /// <summary>The text of every element the expression matches, or one attribute of each.</summary>
    /// <param name="html">The markup of the page as the browser rendered it.</param>
    /// <param name="expression">The CSS selector or XPath that names the elements.</param>
    /// <param name="attribute">The attribute to return instead of the text, or null for the text.</param>
    /// <param name="address">Where the page was read from, which relative links are resolved against.</param>
    /// <param name="limit">How many pieces to return at most.</param>
    Selection Take(string html, Expression expression, string? attribute, Uri address, int limit);
}
