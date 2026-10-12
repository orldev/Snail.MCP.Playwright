using System.Text;
using BenchmarkDotNet.Attributes;
using Snail.MCP.Playwright.Adapters;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Domain;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Text;

namespace Snail.MCP.Playwright.Benchmarks;

/// <summary>
/// What a tool call does in-process once the browser has answered: playwright_select taking the rows of a table out of a
/// page by CSS and by XPath, and every read deciding whether an address is one this server may visit. The page is
/// generated; no browser is started.
/// </summary>
[MemoryDiagnoser]
public class PageBenchmarks
{
    private static readonly Uri Page = new("https://example.org/catalogue/index.html");

    private readonly MarkupSelection _selection = new(new TextExtraction());

    private readonly AddressRules _rules = new(new ServerConfig());

    private string _html = string.Empty;

    private Link[] _links = [];

    [GlobalSetup]
    public void Setup()
    {
        var html = new StringBuilder("<html><body><h1>Prices</h1><table id=\"prices\"><tr><th>Grain</th><th>Price</th><th>Link</th></tr>");

        for (var row = 0; row < 300; row++)
            html.Append($"<tr><td>Grain {row}</td><td>{200 + row}</td><td><a href=\"/docs/grain-{row}\">grain {row}</a></td></tr>");

        html.Append("</table></body></html>");
        _html = html.ToString();

        string[] addresses = ["https://example.org/a", "http://localhost:3000/", "https://10.0.0.7/admin", "http://printer.local/", "https://[::1]/", "https://docs.example.com/page?x=1", "http://192.168.1.1/", "ftp://example.org/file"];
        _links = [.. Enumerable.Range(0, 256).Select(at => Link.Of(addresses[at % addresses.Length]))];
    }

    [Benchmark]
    public int SelectCss() => _selection.Take(_html, Expression.Css("table#prices tr"), null, Page, 200).Pieces.Count;

    [Benchmark]
    public int SelectXPath() => _selection.Take(_html, Expression.XPath("//table[@id='prices']//a"), "href", Page, 200).Pieces.Count;

    [Benchmark]
    public int CheckAddresses() => _links.Count(link => _rules.Refusal(link) is null);
}
