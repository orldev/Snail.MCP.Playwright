namespace Snail.MCP.Playwright.Tests.Support;

/// <summary>A test that needs the real browser container, skipped — not passed — where there is none.</summary>
/// <remarks>
/// The address comes from <c>SNAIL_LIVE_BROWSER_SERVER</c>, the variable the toolkit's own live tests read,
/// so one exported variable runs both suites against one browser:
/// <c>SNAIL_LIVE_BROWSER_SERVER=ws://localhost:3000/ dotnet test</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class LiveFactAttribute : FactAttribute
{
    public const string Variable = "SNAIL_LIVE_BROWSER_SERVER";

    public LiveFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Server))
            Skip = $"needs the browser container: set {Variable} to its address, such as ws://localhost:3000/";
    }

    public static string? Server => Environment.GetEnvironmentVariable(Variable);

    /// <summary>The settings that point the server at the live browser instead of at nothing.</summary>
    public static (string Name, string Value)[] Browser =>
        [("WEB__SCRAPING__BROWSER_SERVER", Server!), ("BROWSER_PROBE_SECONDS", "60")];
}

/// <summary>The live tests share one browser and run one class at a time.</summary>
/// <remarks>
/// xUnit runs classes in parallel, and several of them opening pages through one browser arrive as a
/// burst, which is not what any of them means to measure.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class LiveBrowser
{
    public const string Name = "live browser";
}
