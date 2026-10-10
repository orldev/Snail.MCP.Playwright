using Snail.MCP.Playwright.Adapters;
using Snail.MCP.Playwright.Domain;
using Snail.Toolkit.Web.Text;

namespace Snail.MCP.Playwright.Tests.Adapters;

public sealed class MarkupSelectionTests
{
    private const string Catalogue =
        """
        <html><body>
          <h1>Prices</h1>
          <table id="prices">
            <tr><th>Grain</th><th>Price</th></tr>
            <tr><td>Wheat</td><td>210</td></tr>
            <tr><td>Barley</td><td>185</td></tr>
          </table>
          <ul><li><a href="/docs/wheat">Wheat</a></li><li><a href="https://elsewhere.example/barley">Barley</a></li></ul>
        </body></html>
        """;

    private static readonly Uri Page = new("https://example.org/catalogue/index.html");

    private readonly MarkupSelection _selection = new(new TextExtraction());

    /// <summary>A row comes back as its cells a space apart — text, the way the page reader would give it, not markup.</summary>
    [Fact]
    public void Take_TheRowsOfATable_ReturnsEachRowAsItsText()
    {
        var selected = _selection.Take(Catalogue, Expression.XPath("//table[@id='prices']//tr"), null, Page, 50);

        Assert.Equal(["Grain Price", "Wheat 210", "Barley 185"], selected.Pieces);
        Assert.Equal(3, selected.Matched);
    }

    /// <summary>A relative link is useless to a caller that has only the piece: it comes back as an address that can be fetched.</summary>
    [Fact]
    public void Take_TheLinksOfAList_ReturnsAbsoluteAddresses()
    {
        var selected = _selection.Take(Catalogue, Expression.XPath("//ul//a"), "href", Page, 50);

        Assert.Equal(["https://example.org/docs/wheat", "https://elsewhere.example/barley"], selected.Pieces);
    }

    [Fact]
    public void Take_MoreMatchesThanAsked_ReturnsTheLimitAndSaysHowManyMatched()
    {
        var selected = _selection.Take(Catalogue, Expression.XPath("//tr"), null, Page, 2);

        Assert.Equal(2, selected.Pieces.Count);
        Assert.Equal(3, selected.Matched);
    }

    /// <summary>Nothing matched and could not be asked are different facts, and only one of them is the caller's mistake.</summary>
    [Fact]
    public void Take_AnExpressionMatchingNothing_IsValidAndEmpty()
    {
        var selected = _selection.Take(Catalogue, Expression.XPath("//article"), null, Page, 50);

        Assert.True(selected.IsValid);
        Assert.Empty(selected.Pieces);
        Assert.Equal(0, selected.Matched);
    }

    [Fact]
    public void Take_AnExpressionThatIsNotXPath_SaysWhatTheParserSaid()
    {
        var selected = _selection.Take(Catalogue, Expression.XPath("//table[@id="), null, Page, 50);

        Assert.False(selected.IsValid);
        Assert.False(string.IsNullOrWhiteSpace(selected.Fault));
    }

    /// <summary>An expression that matched the whole body is a mistake, not a request for the page.</summary>
    [Fact]
    public void Take_APieceLongerThanAPiece_IsCutAndSaysSo()
    {
        string page = $"<html><body><p>{new string('a', Limits.SelectedCharacters * 2)}</p></body></html>";

        var selected = _selection.Take(page, Expression.XPath("//p"), null, Page, 50);

        Assert.EndsWith("…", selected.Pieces[0], StringComparison.Ordinal);
        Assert.Equal(Limits.SelectedCharacters + 1, selected.Pieces[0].Length);
    }

    /// <summary>The same row a browser's CSS would name comes back the same way an XPath brings it.</summary>
    [Fact]
    public void Take_ACssSelector_ReturnsTheSameRowsAsTheXPath()
    {
        var selected = _selection.Take(Catalogue, Expression.Css("table#prices tr"), null, Page, 50);

        Assert.Equal(["Grain Price", "Wheat 210", "Barley 185"], selected.Pieces);
    }

    [Fact]
    public void Take_ACssSelectorForAnAttribute_ReturnsAbsoluteAddresses()
    {
        var selected = _selection.Take(Catalogue, Expression.Css("ul a"), "href", Page, 50);

        Assert.Equal(["https://example.org/docs/wheat", "https://elsewhere.example/barley"], selected.Pieces);
    }

    [Fact]
    public void Take_ASelectorThatIsNotCss_SaysWhatTheParserSaid()
    {
        var selected = _selection.Take(Catalogue, Expression.Css("table[id="), null, Page, 50);

        Assert.False(selected.IsValid);
        Assert.False(string.IsNullOrWhiteSpace(selected.Fault));
    }
}
