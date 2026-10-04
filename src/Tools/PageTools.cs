using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Tools.Prompts;

namespace Snail.MCP.Playwright.Tools;

/// <summary>What pages say: an address in, its text out — one page, several, or a site.</summary>
[McpServerToolType]
public sealed class PageTools(PageReading pages, BatchReading batches, SiteReading sites)
{
    /// <summary>Reads one page and returns a piece of its text.</summary>
    [McpServerTool(Name = "playwright_fetch", ReadOnly = true, OpenWorld = true)]
    [Description(ToolDescriptions.Fetch)]
    public async Task<CallToolResult> FetchAsync(
        [Description(ToolDescriptions.FetchUrl)] string url,
        [Description(ToolDescriptions.FetchFrom)] int from = 0,
        [Description(ToolDescriptions.FetchCharacters)] int maxCharacters = Limits.DefaultCharacters,
        CancellationToken cancellationToken = default) =>
        ToolResponse.From(await pages.ReadAsync(url, from, maxCharacters, cancellationToken));

    /// <summary>Reads several pages and returns a piece of each.</summary>
    [McpServerTool(Name = "playwright_fetch_many", ReadOnly = true, OpenWorld = true)]
    [Description(ToolDescriptions.FetchMany)]
    public async Task<CallToolResult> FetchManyAsync(
        [Description(ToolDescriptions.FetchManyUrls)] string[] urls,
        [Description(ToolDescriptions.FetchManyCharacters)] int maxCharactersEach = Limits.DefaultCharactersEach,
        CancellationToken cancellationToken = default) =>
        ToolResponse.From(await batches.ReadAsync(urls, maxCharactersEach, cancellationToken));

    /// <summary>Walks a site and returns a piece of every page it read.</summary>
    [McpServerTool(Name = "playwright_fetch_site", ReadOnly = true, OpenWorld = true)]
    [Description(ToolDescriptions.FetchSite)]
    public async Task<CallToolResult> FetchSiteAsync(
        [Description(ToolDescriptions.FetchSiteUrl)] string url,
        [Description(ToolDescriptions.FetchSitePages)] int maxPages = Limits.DefaultSitePages,
        [Description(ToolDescriptions.FetchSiteDepth)] int maxDepth = Limits.DefaultSiteDepth,
        [Description(ToolDescriptions.FetchSiteSeconds)] int seconds = Limits.DefaultSiteSeconds,
        [Description(ToolDescriptions.FetchSiteCharacters)] int maxCharactersEach = Limits.DefaultSiteCharactersEach,
        CancellationToken cancellationToken = default) =>
        ToolResponse.From(await sites.ReadAsync(url, maxPages, maxDepth, seconds, maxCharactersEach, cancellationToken));
}
