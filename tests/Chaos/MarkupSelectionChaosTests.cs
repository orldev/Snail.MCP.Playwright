using Snail.MCP.Playwright.Adapters;
using Snail.MCP.Playwright.Domain;
using Snail.Toolkit.Web.Text;

namespace Snail.MCP.Playwright.Tests.Chaos;

/// <summary>
/// Breaks the promise that an expression which cannot be asked is answered rather than thrown: an XPath that
/// is valid and returns something other than elements, and a CSS selector a browser takes and Fizzler does not.
/// A throw here leaves the tool boundary as an exception, not as the refusal the description promises.
/// </summary>
public sealed class MarkupSelectionChaosTests
{
    private const string Markup = "<html><body><h1>Prices</h1><table><tr><td><a href='/x'>Wheat</a></td><td>210</td></tr></table></body></html>";

    private static readonly Uri Page = new("https://example.org/catalogue");

    private readonly MarkupSelection _selection = new(new TextExtraction());

    [Theory]
    [InlineData("count(//a)")]
    [InlineData("string(//h1)")]
    [InlineData("//a/@href")]
    [InlineData("1 + 1")]
    [InlineData("true()")]
    [InlineData("//td[position() > 'x']")]
    public void Take_AnXPathThatIsValidAndNotANodeSet_AnswersInsteadOfThrowing(string xpath)
    {
        var thrown = Record.Exception(() => _selection.Take(Markup, Expression.XPath(xpath), null, Page, 50));

        Assert.Null(thrown);
    }

    [Theory]
    [InlineData("td:has(a)")]
    [InlineData(":is(td, th)")]
    [InlineData("td::text")]
    [InlineData("td:nth-child(2n+1 of .x)")]
    [InlineData("a[href^='/x' i]")]
    [InlineData("tr > ")]
    [InlineData("")]
    public void Take_ACssSelectorABrowserTakesAndTheParserMayNot_AnswersInsteadOfThrowing(string css)
    {
        var thrown = Record.Exception(() => _selection.Take(Markup, Expression.Css(css), null, Page, 50));

        Assert.Null(thrown);
    }
}
