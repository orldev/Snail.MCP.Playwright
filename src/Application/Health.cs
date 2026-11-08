using Microsoft.Extensions.Options;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Sdk.Mcp.Hosting;
using Snail.Toolkit.Web.Scraping;
using Snail.Toolkit.Web.Scraping.Browsers;
using Snail.Toolkit.Web.Search;

namespace Snail.MCP.Playwright.Application;

/// <summary>Whether the server can do its work right now: the browser it reads through and the engines it asks.</summary>
/// <remarks>
/// A browser that is not running is reported, not thrown: the question was «what state is this server
/// in», and «the browser does not answer» is the answer to it rather than a failure to give one.
/// </remarks>
public sealed class Health(
    IBrowserPool browsers,
    IBrowserServer door,
    IEnumerable<ISearchProvider> engines,
    IOptions<ScrapingOptions> scraping,
    ServerConfig config,
    McpEndpoint endpoint,
    TimeProvider clock)
{
    private TimeSpan Patience => TimeSpan.FromSeconds(config.BrowserProbeSeconds);

    /// <summary>The state of the server, of the browser behind it and of the search it offers.</summary>
    public async Task<Answer<JsonNode>> ReportAsync(CancellationToken cancellationToken = default) =>
        Answer.Ok<JsonNode>(new JsonObject
        {
            ["server"] = new JsonObject
            {
                ["name"] = ServerIdentity.NameOf(typeof(Health).Assembly),
                ["version"] = ServerIdentity.VersionOf(typeof(Health).Assembly),
                ["transport"] = ToolAnswer.Word(endpoint.Transport)
            },
            ["browser"] = await BrowserAsync(cancellationToken),
            ["search"] = new JsonObject
            {
                ["engines"] = new JsonArray([.. engines.Select(engine => (JsonNode)engine.Name)])
            }
        });

    /// <summary>Whether the browser answers, asked the cheap way: a knock at the browser server's address, no browser rented.</summary>
    /// <remarks>
    /// The health check is what an orchestrator restarts a container by, and a rent waits for a permit the
    /// page loads in flight hold: asked through the pool, a busy server was reported dead. The knock takes no
    /// permit. A server that launches its own browser has no address to knock at and is still asked by renting.
    /// </remarks>
    public async Task<bool> IsBrowserReachableAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scraping.Value.BrowserServer))
            return (await BrowserAsync(cancellationToken))["isReachable"]!.GetValue<bool>();

        return await door.IsAnsweringAsync(Patience, cancellationToken);
    }

    private async Task<JsonObject> BrowserAsync(CancellationToken cancellationToken)
    {
        long asked = clock.GetTimestamp();

        using var patience = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        patience.CancelAfter(Patience);

        try
        {
            await using var lease = await browsers.RentAsync(patience.Token);

            return Described(lease.Browser.Version, null, clock.GetElapsedTime(asked));
        }
        catch (Exception refused) when (IsAnAnswer(refused, cancellationToken))
        {
            return Described(null, refused.Message, clock.GetElapsedTime(asked));
        }
    }

    private JsonObject Described(string? version, string? error, TimeSpan took) => new()
    {
        ["server"] = scraping.Value.BrowserServer,
        ["engine"] = scraping.Value.Engine.ToString(),
        ["isReachable"] = error is null,
        ["version"] = version,
        ["milliseconds"] = (long)took.TotalMilliseconds,
        ["error"] = error,
        ["hint"] = error is null ? null : Messages.BrowserUnreachable(scraping.Value.BrowserServer)
    };

    /// <summary>Whether a failure to reach the browser is itself the report, as everything but the caller's own cancellation is.</summary>
    /// <remarks>
    /// Wider than the toolkit's own notion of a browser failure on purpose. Found on 2026-10-04 in the first
    /// image built: the driver's node binary was not executable by the container's user, the process could
    /// not be started, and that arrived as a <c>Win32Exception</c> — which the one tool meant to explain a
    /// dead browser then answered with a stack trace. Whatever stops the browser from being reached, saying
    /// so is this tool's whole job.
    /// </remarks>
    private static bool IsAnAnswer(Exception refused, CancellationToken cancellationToken) =>
        refused is not OperationCanceledException || !cancellationToken.IsCancellationRequested;
}
