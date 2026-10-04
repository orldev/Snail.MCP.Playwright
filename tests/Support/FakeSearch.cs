using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Search;

namespace Snail.MCP.Playwright.Tests.Support;

/// <summary>A search that answers what the test told it to, and remembers what it was asked.</summary>
internal sealed class FakeSearch(SearchOutcome outcome) : IWebSearch
{
    public List<string> Asked { get; } = [];

    public TimeSpan? Budget { get; private set; }

    public static SearchResult Result(string title, string url, string snippet = "a snippet", string engine = "Bing") => new()
    {
        Title = title,
        Address = Link.Of(url),
        Snippet = snippet,
        Provider = engine
    };

    public static SearchAttempt Answered(string engine, params SearchResult[] results) => new()
    {
        Provider = engine,
        Query = "asked",
        Duration = TimeSpan.FromMilliseconds(120),
        Results = results
    };

    public static SearchAttempt Failed(string engine, SearchFailure failure) => new()
    {
        Provider = engine,
        Query = "asked",
        Duration = TimeSpan.FromMilliseconds(5),
        Failure = failure
    };

    public static FakeSearch Finding(params SearchResult[] results) =>
        new(new SearchOutcome(results, [Answered("Bing", results)]));

    public Task<SearchOutcome> FindAsync(IEnumerable<string> queries, CancellationToken ct = default) =>
        FindAsync(queries, TimeSpan.Zero, ct);

    public Task<SearchOutcome> FindAsync(IEnumerable<string> queries, TimeSpan budget, CancellationToken ct = default)
    {
        Asked.AddRange(queries);
        Budget = budget;

        return Task.FromResult(outcome);
    }

    public IAsyncEnumerable<SearchAttempt> StreamAsync(IEnumerable<string> queries, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<SearchAttempt> StreamAsync(IEnumerable<string> queries, TimeSpan budget, CancellationToken ct = default) =>
        throw new NotSupportedException();
}
