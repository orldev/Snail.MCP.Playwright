using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Tools.Prompts;

namespace Snail.MCP.Playwright.Tools;

/// <summary>The state of the server and of the browser behind it.</summary>
[McpServerToolType]
public sealed class DiagnosticsTools(Health health)
{
    /// <summary>Reports the server, the browser and the search engines.</summary>
    [McpServerTool(Name = "playwright_diagnose", ReadOnly = true, OpenWorld = false)]
    [Description(ToolDescriptions.Diagnose)]
    public async Task<CallToolResult> DiagnoseAsync(CancellationToken cancellationToken = default) =>
        ToolAnswer.From(await health.ReportAsync(cancellationToken), AnswerShape.None);
}
