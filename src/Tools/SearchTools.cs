using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Tools.Prompts;

namespace Snail.MCP.Playwright.Tools;

/// <summary>Where something is written about: queries in, links out.</summary>
[McpServerToolType]
public sealed class SearchTools(LinkSearch search)
{
    /// <summary>Asks the engines and returns the links they offer.</summary>
    [McpServerTool(Name = "playwright_search", ReadOnly = true, OpenWorld = true)]
    [Description(ToolDescriptions.Search)]
    public async Task<CallToolResult> SearchAsync(
        [Description(ToolDescriptions.SearchQueries)] string[] queries,
        [Description(ToolDescriptions.SearchLimit)] int limit = Limits.DefaultResults,
        CancellationToken cancellationToken = default) =>
        ToolAnswer.From<LinksFound, LinksFound>(await search.FindAsync(queries, limit, cancellationToken), AnswerFields.Of, AnswerFields.Of, AnswerShape.None);
}
