using Microsoft.Extensions.Hosting;
using Snail.MCP.Playwright.Extensions;

await McpServerHost.Of(args).RunAsync();
