using System.Runtime.CompilerServices;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Tests.Support;

/// <summary>A crawler that reads through a fake reader and answers a walk with the report a test wrote.</summary>
internal sealed class FakeCrawler(FakeReader reader) : ICrawler
{
    public CrawlScope? Walked { get; private set; }

    public Func<CrawlScope, CrawlReport> Report { get; set; } = scope => Reported(scope, CrawlStop.Exhausted);

    public static CrawlReport Reported(CrawlScope scope, CrawlStop stop, params Outcome<CrawlPage, PageFailure>[] pages) => new()
    {
        Start = scope.Start,
        Pages = pages,
        Stop = stop,
        IsFromSitemap = true,
        Elapsed = TimeSpan.FromSeconds(12)
    };

    public static Outcome<CrawlPage, PageFailure> Page(string url, string text, int depth = 0) =>
        Outcome<CrawlPage, PageFailure>.Success(new CrawlPage(Link.Of(url), depth, text, DateTimeOffset.UnixEpoch) { Title = "A page" });

    public static Outcome<CrawlPage, PageFailure> Failure(string url, FailureKind kind, int? status = null) =>
        Outcome<CrawlPage, PageFailure>.Failed(new PageFailure(kind, "the site said no") { Address = Link.Of(url), StatusCode = status });

    public async IAsyncEnumerable<Outcome<PageContent, PageFailure>> StreamAsync(
        IEnumerable<Link> addresses,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var address in addresses)
            yield return await reader.ReadAsync(address, ct);
    }

    public async Task<IReadOnlyList<Outcome<PageContent, PageFailure>>> ReadAllAsync(IEnumerable<Link> addresses, CancellationToken ct = default)
    {
        var read = new List<Outcome<PageContent, PageFailure>>();

        await foreach (var outcome in StreamAsync(addresses, ct))
            read.Add(outcome);

        return read;
    }

    public CrawlScope ScopeOf(Link start) => new()
    {
        Start = start,
        MaxPages = 500,
        Budget = TimeSpan.FromMinutes(40),
        MaxDepth = 3,
        IncludesSubdomains = false,
        IsSitemapPreferred = true,
        IsNewestFirst = true
    };

    public Task<CrawlReport> CrawlAsync(CrawlScope scope, CancellationToken ct = default)
    {
        Walked = scope;

        return Task.FromResult(Report(scope));
    }
}
