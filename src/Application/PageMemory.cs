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
/// <para>
/// One lock instead of a concurrent dictionary: the trim decides on the whole memory at once, and a trim
/// run on a snapshot that other sessions had already outgrown removed pages kept a moment ago — measured
/// by the audit of 2026-10-16, pages lost in every round of 64 kept at once. The work under the lock is a
/// sort of at most 256 entries.
/// </para>
/// <para>
/// Three ceilings, applied newest first: a page too old is gone, then the pages past the count, then the
/// pages past the bytes. The page kept last stays whatever its size, because forgetting it would make every
/// piece of it a visit — which is what the memory exists to prevent.
/// </para>
/// </remarks>
public sealed class PageMemory(ServerConfig config, TimeProvider clock)
{
    private readonly Dictionary<Link, PageContent> _pages = [];

    private readonly Lock _gate = new();

    private TimeSpan Lifetime => TimeSpan.FromMinutes(config.PageMemoryMinutes);

    private long Ceiling => config.PageMemoryMegabytes * 1024L * 1024L;

    /// <summary>The page as it was read, or null when it was not, or was read too long ago to trust.</summary>
    public PageContent? Of(Link address)
    {
        lock (_gate)
            return _pages.TryGetValue(address, out var page) && IsFresh(page) ? page : null;
    }

    /// <summary>Remembers a page under the address it was asked for, making room when the memory is full.</summary>
    public void Keep(PageContent page)
    {
        if (Lifetime <= TimeSpan.Zero)
            return;

        lock (_gate)
        {
            _pages[page.Requested] = page;
            Forget();
        }
    }

    private bool IsFresh(PageContent page) => clock.GetUtcNow() - page.FetchedAt < Lifetime;

    private void Forget()
    {
        var fresh = _pages.Values.Where(IsFresh);
        var newest = fresh.OrderByDescending(page => page.FetchedAt).Take(config.PageMemoryPages);
        var kept = WithinTheCeiling(newest);
        var forgotten = _pages.Keys.Where(address => !kept.Contains(address)).ToList();

        foreach (var address in forgotten)
            _pages.Remove(address);
    }

    /// <summary>The addresses of the leading pages whose markup and text fit under the ceiling together, and always of the first.</summary>
    private HashSet<Link> WithinTheCeiling(IEnumerable<PageContent> newestFirst)
    {
        var kept = new HashSet<Link>();
        long held = 0;

        foreach (var page in newestFirst)
        {
            held += Size(page);

            if (held > Ceiling && kept.Count > 0)
                break;

            kept.Add(page.Requested);
        }

        return kept;
    }

    private static long Size(PageContent page) => (page.Html.Length + page.Text.Length) * sizeof(char);
}
