using Snail.MCP.Playwright.Tools.Prompts;

namespace Snail.MCP.Playwright.Tests.Contracts;

/// <summary>The server over HTTP as a program, the way the image starts it: its address read from its variables, not handed in.</summary>
/// <remarks>
/// The in-process tests build the host themselves, so only a process shows that the program reads
/// <c>SNAIL_MCP_PLAYWRIGHT_URL</c> and <c>_TRANSPORT</c> into an HTTP endpoint. These tests run with no browser behind
/// the server (<see cref="Deployment.NoBrowser"/>), and its <c>/healthz</c> answers 503 while no browser answers, which is
/// the point of it, so they wait for the MCP endpoint instead (<c>readiness: null</c>). The default readiness, with a
/// browser that answers, is <see cref="HealthContractTests"/>'.
/// </remarks>
public sealed class HttpProgramContractTests : IAsyncLifetime
{
    private HttpServerProgram _server = null!;

    public async Task InitializeAsync() =>
        _server = await HttpServerProgram.StartAsync(
            "Snail.MCP.Playwright",
            "SNAIL_PLAYWRIGHT_SERVER",
            Deployment.Variables().ToDictionary(variable => variable.Key, variable => (string?)variable.Value),
            readiness: null);

    public async Task DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task Diagnose_AsAProgramAtAnAddress_SaysItSpeaksHttp()
    {
        await using var client = await _server.SessionAsync();

        var answer = await client.CallToolAsync("playwright_diagnose", new Dictionary<string, object?>());

        Assert.True(answer.IsOk(), answer.Text());
        Assert.Equal(ServerGuidance.Instructions, client.ServerInstructions);
        Assert.Equal("http", answer.Data()!["server"]!["transport"]!.GetValue<string>());
    }

    [Fact]
    public async Task Tools_GrantedOne_ListThatOneAlone()
    {
        await using var client = await _server.SessionAsync("tool=playwright_fetch");

        var listed = await client.ListToolsAsync();

        Assert.Equal(["playwright_fetch"], listed.Select(tool => tool.Name));
    }

    [Fact]
    public async Task Endpoint_AsServed_RefusesBrowsersOnly() =>
        Assert.Empty(await HttpGuards.ProblemsAsync(_server.Endpoint));
}
