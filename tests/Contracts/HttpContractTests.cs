using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Domain;
using Snail.MCP.Playwright.Extensions;
using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web.Search;

namespace Snail.MCP.Playwright.Tests.Contracts;

/// <summary>The server at an address, as the platform meets it: what it announces, what it lists and whom it refuses.</summary>
public sealed class HttpContractTests
{
    [Fact]
    public async Task Server_AtAnAddress_AnnouncesItsNameVersionAndInstructions()
    {
        await using var server = await ListeningServer.StartAsync();
        await using var client = await server.ConnectAsync();

        Assert.Equal(ThisAssembly.Name, client.ServerInfo.Name);
        Assert.Equal(ThisAssembly.Version, client.ServerInfo.Version);
        Assert.Equal(ServerGuidance.Instructions, client.ServerInstructions);
    }

    /// <summary>A tool declared and never registered exists in the code and nowhere a model can reach.</summary>
    [Fact]
    public async Task Server_AtAnAddress_ListsExactlyTheToolsTheAssemblyDeclares()
    {
        await using var server = await ListeningServer.StartAsync();
        await using var client = await server.ConnectAsync();

        var listed = await client.ListToolsAsync();

        Assert.Equal(Catalog.Names, [.. listed.Select(tool => tool.Name).Order(StringComparer.Ordinal)]);
    }

    /// <summary>The question was what state the server is in, and «no browser» is an answer to it, not a failure to answer.</summary>
    [Fact]
    public async Task Diagnose_WithNoBrowserRunning_ReportsItUnreachableAndSaysWhatToStart()
    {
        await using var server = await ListeningServer.StartAsync();
        await using var client = await server.ConnectAsync();

        var answer = await client.CallToolAsync("playwright_diagnose", new Dictionary<string, object?>());

        Assert.NotEqual(true, answer.IsError);
        Assert.False(answer.Data()["browser"]!["isReachable"]!.GetValue<bool>());
        Assert.Equal(Deployment.NoBrowser, answer.Data()["browser"]!["server"]!.GetValue<string>());
        Assert.Contains(BrowserDefaults.Image, answer.Data()["browser"]!["hint"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>
    /// The platform appends the role's tools to the address and expects the server to hold the line: a
    /// list that the server ignores is a list in name only.
    /// </summary>
    [Fact]
    public async Task Call_ToAToolTheSessionWasNotGranted_IsRefusedAtTheDoor()
    {
        await using var server = await ListeningServer.StartAsync();
        await using var client = await server.ConnectAsync("?tool=playwright_search");

        var refused = await client.CallToolAsync("playwright_diagnose", new Dictionary<string, object?>());

        Assert.Equal(true, refused.IsError);
        Assert.Contains("was not granted", refused.Text(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Call_FromASessionNamingNoTool_MayUseThemAll()
    {
        await using var server = await ListeningServer.StartAsync();
        await using var client = await server.ConnectAsync();

        Assert.NotEqual(true, (await client.CallToolAsync("playwright_diagnose", new Dictionary<string, object?>())).IsError);
    }

    /// <summary>A refusal is data with the error flag set, never a transport exception the client cannot read.</summary>
    [Fact]
    public async Task Fetch_OfAnAddressInsideTheDeployment_IsRefusedAsAToolError()
    {
        await using var server = await ListeningServer.StartAsync();
        await using var client = await server.ConnectAsync();

        var refused = await client.CallToolAsync("playwright_fetch", new Dictionary<string, object?> { ["url"] = "http://minio:9000/" });

        Assert.Equal(true, refused.IsError);
        Assert.Contains("not on the public web", refused.Text(), StringComparison.Ordinal);
    }

    /// <summary>
    /// The moves arrive as JSON written by a model, and only the real server shows whether they are read the
    /// way the description promises: a move that is understood well enough to be refused by name was read.
    /// </summary>
    [Fact]
    public async Task Perform_WithMovesSentAsJson_ReadsThemAndNamesTheOneThatIsWrong()
    {
        await using var server = await ListeningServer.StartAsync();
        await using var client = await server.ConnectAsync();

        var refused = await client.CallToolAsync("playwright_perform", new Dictionary<string, object?>
        {
            ["url"] = "https://example.org/search",
            ["moves"] = new object[]
            {
                new Dictionary<string, object?> { ["action"] = "type", ["label"] = "Query", ["value"] = "grain" },
                new Dictionary<string, object?> { ["action"] = "dance", ["css"] = "#go" }
            }
        });

        Assert.Equal(true, refused.IsError);
        Assert.Contains("move 2: 'dance' is not a move", refused.Text(), StringComparison.Ordinal);
    }

    /// <summary>The fields arrive as JSON written by a model; a pair refused by name before any visit was read the way the description promises.</summary>
    [Fact]
    public async Task ExtractFields_WithFieldsSentAsJson_ReadsThemAndNamesTheOneThatIsWrong()
    {
        await using var server = await ListeningServer.StartAsync();
        await using var client = await server.ConnectAsync();

        var refused = await client.CallToolAsync("playwright_extract_fields", new Dictionary<string, object?>
        {
            ["url"] = "https://example.org/prices",
            ["fields"] = new object[]
            {
                new Dictionary<string, object?> { ["name"] = "price", ["css"] = new[] { "td.price" } },
                new Dictionary<string, object?> { ["name"] = "price", ["xpath"] = new[] { "//td[2]" }, ["many"] = true }
            }
        });

        Assert.Equal(true, refused.IsError);
        Assert.Contains("two fields are named 'price'", refused.Text(), StringComparison.Ordinal);
    }

    /// <summary>The page is paid for once: a structured copy beside the text would hand a model every page twice.</summary>
    [Fact]
    public async Task Answer_OfAnyTool_CarriesItsPayloadOnce()
    {
        await using var server = await ListeningServer.StartAsync();
        await using var client = await server.ConnectAsync();

        var answer = await client.CallToolAsync("playwright_diagnose", new Dictionary<string, object?>());

        Assert.Single(answer.Content);
        Assert.Null(answer.StructuredContent);
    }

    /// <summary>A page in a browser always sends an Origin and an agent's client never does, so the header alone tells them apart.</summary>
    [Fact]
    public async Task Request_CarryingAnOrigin_IsRefusedBeforeTheServerReadsIt()
    {
        await using var server = await ListeningServer.StartAsync();
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Origin", "https://page.example");

        var answer = await client.PostAsync(new Uri($"{server.Address}{McpServerHost.Path}"), new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, answer.StatusCode);
    }

    [Fact]
    public async Task Health_WithNoBrowserRunning_AnswersUnavailable()
    {
        await using var server = await ListeningServer.StartAsync();
        using var client = new HttpClient();

        var answer = await client.GetAsync(new Uri($"{server.Address}{McpServerHost.HealthPath}"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, answer.StatusCode);
    }

    [Fact]
    public async Task Search_WithNothingConfigured_AsksTheDefaultEngines()
    {
        await using var server = await ListeningServer.StartAsync();

        Assert.Equal(
            [SearchEngines.Bing, SearchEngines.Brave, SearchEngines.DuckDuckGo],
            [.. server.Services.GetServices<ISearchProvider>().Select(engine => engine.Name).Order(StringComparer.Ordinal)]);
    }

    /// <summary>The toolkit registers DuckDuckGo with the search itself; a deployment that left it out has to be heard anyway.</summary>
    [Fact]
    public async Task Search_WithEnginesNamed_AsksThoseAndNoOthers()
    {
        await using var server = await ListeningServer.StartAsync(("ENGINES", "bing,google"));

        Assert.Equal(
            [SearchEngines.Bing, SearchEngines.Google],
            [.. server.Services.GetServices<ISearchProvider>().Select(engine => engine.Name).Order(StringComparer.Ordinal)]);
    }
}
