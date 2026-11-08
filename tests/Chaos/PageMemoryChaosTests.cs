using Microsoft.Extensions.Time.Testing;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Tests.Chaos;

/// <summary>
/// Breaks the memory of pages two ways: many sessions keeping pages at once, where one session's trim ran on a
/// snapshot the others had already outgrown; and pages of the size the toolkit lets through, where a memory
/// bounded in pages and not in bytes held more than the container is given.
/// </summary>
public sealed class PageMemoryChaosTests
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static PageContent Page(string url, int characters = 10, int secondsAfterNoon = 0) =>
        new(Link.Of(url), new string('h', characters), new string('t', characters), Noon.AddSeconds(secondsAfterNoon));

    /// <summary>
    /// Every page fits — 64 pages, a memory of 64 — so none may be forgotten. Before the lock, Keep() trimmed on a
    /// list it read before the other threads' pages were in, and a page kept a moment ago was removed by a
    /// neighbour's trim: the next piece of it was a second visit to the site.
    /// </summary>
    [Fact]
    public async Task Keep_FromManySessionsAtOnce_ForgetsNoPageThatFits()
    {
        var config = new ServerConfig { PageMemoryPages = 64 };
        var lost = new List<string>();

        for (int round = 0; round < 40; round++)
        {
            var memory = new PageMemory(config, TimeProvider.System);
            var pages = Enumerable.Range(0, 64).Select(index => new PageContent(Link.Of($"https://example.org/{round}/{index}"), "h", "t", DateTimeOffset.UtcNow)).ToArray();

            await Task.WhenAll(pages.Select(page => Task.Run(() => memory.Keep(page))));

            lost.AddRange(pages.Where(page => memory.Of(page.Requested) is null).Select(page => page.Requested.Text));
        }

        Assert.Empty(lost);
    }

    /// <summary>
    /// The toolkit's reader caps nothing by default (MaxContentCharacters 0): 32 pages of 2 million characters of
    /// markup held 257 MB on 2026-10-16, half of the 512 MB the deployment gives the whole server. A page is 4 bytes
    /// a character here (markup and text, UTF-16), so 2 million characters are 8 MB, and a ceiling of 20 MB holds
    /// two of them.
    /// </summary>
    [Fact]
    public void Keep_PagesPastTheByteCeiling_ForgetsTheOldestFirst()
    {
        var memory = new PageMemory(new ServerConfig { PageMemoryPages = 256, PageMemoryMegabytes = 20 }, new FakeTimeProvider(Noon.AddMinutes(1)));

        for (int index = 0; index < 8; index++)
            memory.Keep(Page($"https://example.org/heavy/{index}", 2_000_000, secondsAfterNoon: index));

        Assert.NotNull(memory.Of(Link.Of("https://example.org/heavy/7")));
        Assert.NotNull(memory.Of(Link.Of("https://example.org/heavy/6")));
        Assert.Null(memory.Of(Link.Of("https://example.org/heavy/5")));
        Assert.Null(memory.Of(Link.Of("https://example.org/heavy/0")));
    }

    /// <summary>Forgetting the one page that is too big would make every piece of it a visit, which the memory exists to prevent.</summary>
    [Fact]
    public void Keep_APageLargerThanTheCeiling_KeepsItAlone()
    {
        var memory = new PageMemory(new ServerConfig { PageMemoryMegabytes = 1 }, new FakeTimeProvider(Noon.AddMinutes(1)));

        memory.Keep(Page("https://example.org/small", secondsAfterNoon: 0));
        memory.Keep(Page("https://example.org/huge", 1_000_000, secondsAfterNoon: 1));

        Assert.NotNull(memory.Of(Link.Of("https://example.org/huge")));
        Assert.Null(memory.Of(Link.Of("https://example.org/small")));
    }
}
