using ModelContextProtocol.Client;
using Snail.MCP.Playwright.Tools.Prompts;

namespace Snail.MCP.Playwright.Tests.Contracts;

/// <summary>The server as a program: the real process, started the way a client starts it.</summary>
/// <remarks>
/// Only a process shows the one thing stdio can get wrong and nothing in-process can — a log line on
/// stdout, which is a broken JSON-RPC stream to the client and an unexplained hang to the person behind it.
/// The process is the build beside the tests, or the publication <c>SNAIL_PLAYWRIGHT_SERVER</c> names.
/// </remarks>
public sealed class StdioContractTests
{
    [Fact]
    public async Task Server_AsAProgram_AnnouncesItselfAndListsItsTools()
    {
        await using var client = await StartedAsync();

        var listed = await client.ListToolsAsync();

        Assert.NotNull(client.ServerInfo);
        Assert.Equal("s3l-mcp-playwright", client.ServerInfo.Name);
        Assert.Equal(ServerGuidance.Instructions, client.ServerInstructions);
        Assert.Equal(Catalog.Names, [.. listed.Select(tool => tool.Name).Order(StringComparer.Ordinal)]);
    }

    [Fact]
    public async Task Diagnose_AsAProgram_SaysItSpeaksStdio()
    {
        await using var client = await StartedAsync();

        var answer = await client.CallToolAsync("playwright_diagnose", new Dictionary<string, object?>());

        Assert.NotEqual(true, answer.IsError);
        Assert.Equal("stdio", answer.Data()!["server"]!["transport"]!.GetValue<string>());
    }

    /// <summary>
    /// The guard rules of every Snail server, on the list a model receives: names in snake case and listed once, the tool
    /// and every parameter described, read-only or destructive stated, every allowed value named, English.
    /// </summary>
    [Fact]
    public async Task Tools_AsListed_KeepTheGuardRules()
    {
        await using var client = await StartedAsync();

        var listed = await client.ListToolsAsync();

        Assert.Empty(ToolGuards.Problems(listed.Select(tool => tool.ProtocolTool)));
    }

    private static Task<McpClient> StartedAsync() =>
        ServerProgram.StartAsync(
            "Snail.MCP.Playwright",
            "SNAIL_PLAYWRIGHT_SERVER",
            Deployment.Variables().ToDictionary(variable => variable.Key, variable => (string?)variable.Value));
}
