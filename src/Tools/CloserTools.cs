using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Tools.Prompts;

namespace Snail.MCP.Playwright.Tools;

/// <summary>What plain reading does not give: pieces of a page by expression, a picture of it, and the page after a form was filled in.</summary>
[McpServerToolType]
public sealed class CloserTools(PageExtraction extraction, PageFields records, PagePictures pictures, PageWalking walking)
{
    /// <summary>Returns what a CSS selector or an XPath matches in a page.</summary>
    [McpServerTool(Name = "playwright_extract", ReadOnly = true, OpenWorld = true)]
    [Description(ToolDescriptions.Extract)]
    public async Task<CallToolResult> ExtractAsync(
        [Description(ToolDescriptions.ExtractUrl)] string url,
        [Description(ToolDescriptions.ExtractXPath)] string? xpath = null,
        [Description(ToolDescriptions.ExtractCss)] string? css = null,
        [Description(ToolDescriptions.ExtractAttribute)] string? attribute = null,
        [Description(ToolDescriptions.ExtractLimit)] int limit = Limits.DefaultSelected,
        CancellationToken cancellationToken = default) =>
        ToolResponse.From(await extraction.SelectAsync(url, xpath, css, attribute, limit, cancellationToken));

    /// <summary>Returns a record of named values from a page, and the selector that found each.</summary>
    [McpServerTool(Name = "playwright_extract_fields", ReadOnly = true, OpenWorld = true)]
    [Description(ToolDescriptions.ExtractFields)]
    public async Task<CallToolResult> ExtractFieldsAsync(
        [Description(ToolDescriptions.ExtractFieldsUrl)] string url,
        [Description(ToolDescriptions.ExtractFieldsFields)] Field[] fields,
        CancellationToken cancellationToken = default) =>
        ToolResponse.From(await records.TakeAsync(url, fields, cancellationToken));

    /// <summary>Returns a picture of a page.</summary>
    [McpServerTool(Name = "playwright_screenshot", ReadOnly = true, OpenWorld = true)]
    [Description(ToolDescriptions.Screenshot)]
    public async Task<CallToolResult> ScreenshotAsync(
        [Description(ToolDescriptions.ScreenshotUrl)] string url,
        [Description(ToolDescriptions.ScreenshotWholePage)] bool wholePage = false,
        CancellationToken cancellationToken = default) =>
        ToolResponse.From(await pictures.TakeAsync(url, wholePage, cancellationToken));

    /// <summary>Makes moves on a page and returns the text of what resulted.</summary>
    /// <remarks>
    /// The one tool here that is not declared read-only: a click submits whatever the form submits, and
    /// what that does on the site's side is not this server's to promise.
    /// </remarks>
    [McpServerTool(Name = "playwright_perform", ReadOnly = false, Destructive = true, OpenWorld = true)]
    [Description(ToolDescriptions.Perform)]
    public async Task<CallToolResult> PerformAsync(
        [Description(ToolDescriptions.PerformUrl)] string url,
        [Description(ToolDescriptions.PerformMoves)] Move[] moves,
        [Description(ToolDescriptions.PerformCharacters)] int maxCharacters = Limits.DefaultCharacters,
        CancellationToken cancellationToken = default) =>
        ToolResponse.From(await walking.PerformAsync(url, moves, maxCharacters, cancellationToken));
}
