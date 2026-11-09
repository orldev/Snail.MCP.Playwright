using Snail.MCP.Playwright.Application;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Tests.Chaos;

/// <summary>
/// Breaks the cut of a page at its start: next is always placed before a surrogate pair, but from is any number a
/// model writes, and one that lands between the halves of a character began the piece with half a character, which
/// the answer carried as U+FFFD.
/// </summary>
public sealed class PieceChaosTests
{
    [Fact]
    public void Of_FromBetweenTheHalvesOfOneCharacter_KeepsTheCharacterWholeAndSaysWhereItStarted()
    {
        var page = new PageContent(Link.Of("https://example.org/"), "<html></html>", "a😀bbbb", DateTimeOffset.UtcNow);

        Assert.True(Piece.Of(page, 2, 500).TryGet(out var piece));

        Assert.DoesNotContain('�', piece.Text);
        Assert.StartsWith("😀", piece.Text, StringComparison.Ordinal);
        Assert.Equal(1, piece.From);
    }
}
