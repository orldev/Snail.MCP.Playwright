using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;
using Snail.Toolkit.Web.Scraping.Browsers;

namespace Snail.MCP.Playwright.Tests.Support;

/// <summary>A page reader that serves the pages a test wrote, and counts every visit it is asked to make.</summary>
internal sealed class FakeReader(TimeProvider? clock = null) : IPageReader
{
    private readonly Dictionary<Link, Outcome<PageContent, PageFailure>> _pages = [];

    public List<Link> Visited { get; } = [];

    public FakeReader Serving(string url, string text, string? title = "A page", string html = "<html></html>")
    {
        var address = Link.Of(url);

        _pages[address] = Outcome<PageContent, PageFailure>.Success(
            new PageContent(address, html, text, (clock ?? TimeProvider.System).GetUtcNow()) { Final = address, Title = title, StatusCode = 200 });

        return this;
    }

    public FakeReader Refusing(string url, FailureKind kind, int? status = null, TimeSpan? retryAfter = null)
    {
        var address = Link.Of(url);

        _pages[address] = Outcome<PageContent, PageFailure>.Failed(
            new PageFailure(kind, "the site said no") { Address = address, StatusCode = status, RetryAfter = retryAfter });

        return this;
    }

    public Task<Outcome<PageContent, PageFailure>> ReadAsync(Link address, CancellationToken ct = default)
    {
        Visited.Add(address);

        return Task.FromResult(_pages.TryGetValue(address, out var known)
            ? known
            : Outcome<PageContent, PageFailure>.Failed(new PageFailure(FailureKind.Navigation, "no such page") { Address = address, StatusCode = 404 }));
    }

    public Task<Outcome<PageContent, PageFailure>> ReadAsync(Link address, Appetite appetite, CancellationToken ct = default) =>
        ReadAsync(address, ct);
}
