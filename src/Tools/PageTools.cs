using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Tools.Prompts;

namespace Snail.MCP.Playwright.Tools;

/// <summary>What pages say: an address in, its text out — one page, several, or a site.</summary>
/// <remarks>
/// Every tool of this server answers in Snail.Sdk's envelope with no structured copy beside the text
/// (<see cref="AnswerShape.None"/>): the payload is the text of a page, and a client that hands the whole result to its
/// model — the platform this server is built for does — would pay for every page twice. A failure carries what was
/// learned before it as its data: the kind of refusal and when to come back, the pages of a batch that went through.
/// </remarks>
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
        ToolAnswer.From(await pages.ReadAsync(url, from, maxCharacters, cancellationToken), AnswerShape.None);

    /// <summary>Reads several pages and returns a piece of each.</summary>
    [McpServerTool(Name = "playwright_fetch_many", ReadOnly = true, OpenWorld = true)]
    [Description(ToolDescriptions.FetchMany)]
    public async Task<CallToolResult> FetchManyAsync(
        [Description(ToolDescriptions.FetchManyUrls)] string[] urls,
        [Description(ToolDescriptions.FetchManyCharacters)] int maxCharactersEach = Limits.DefaultCharactersEach,
        CancellationToken cancellationToken = default) =>
        ToolAnswer.From(await batches.ReadAsync(urls, maxCharactersEach, cancellationToken), AnswerShape.None);

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
        ToolAnswer.From(await sites.ReadAsync(url, maxPages, maxDepth, seconds, maxCharactersEach, cancellationToken), AnswerShape.None);
}
