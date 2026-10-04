using System.Collections.Concurrent;
using Snail.MCP.Playwright.Configuration;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>The pages read a moment ago, kept so the rest of one is served without visiting the site again.</summary>
/// <remarks>
/// An answer carries a piece of a page, and a caller that wants the next piece asks again. Without this
/// each of those calls would be another visit: the site sees a client reloading one page over and over,
/// and the politeness gate makes the caller wait for the privilege. A long page costs one visit here
/// however many pieces it is read in.
/// </remarks>
public sealed class PageMemory(ServerConfig config, TimeProvider clock)
{
    private readonly ConcurrentDictionary<Link, PageContent> _pages = new();

    private TimeSpan Lifetime => TimeSpan.FromMinutes(config.PageMemoryMinutes);

    /// <summary>The page as it was read, or null when it was not, or was read too long ago to trust.</summary>
    public PageContent? Of(Link address) =>
        _pages.TryGetValue(address, out var page) && IsFresh(page) ? page : null;

    /// <summary>Remembers a page under the address it was asked for, making room when the memory is full.</summary>
    public void Keep(PageContent page)
    {
        if (Lifetime <= TimeSpan.Zero)
            return;

        _pages[page.Requested] = page;

        Forget();
    }

    private bool IsFresh(PageContent page) => clock.GetUtcNow() - page.FetchedAt < Lifetime;

    private void Forget()
    {
        var kept = _pages.Values
            .Where(IsFresh)
            .OrderByDescending(page => page.FetchedAt)
            .Take(config.PageMemoryPages)
            .Select(page => page.Requested)
            .ToHashSet();

        foreach (var address in _pages.Keys.Where(address => !kept.Contains(address)))
            _pages.TryRemove(address, out _);
    }
}
