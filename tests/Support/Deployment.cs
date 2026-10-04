using Microsoft.Extensions.Configuration;
using Snail.MCP.Playwright.Configuration;

namespace Snail.MCP.Playwright.Tests.Support;

/// <summary>The environment a test starts the server in, written the way a deployment writes it.</summary>
/// <remarks>
/// Every test that builds the server names a browser address nothing listens at. The default address is
/// where a developer's own browser container runs, and a suite that reached it would pass or fail by what
/// happens to be running on the machine.
/// </remarks>
internal static class Deployment
{
    /// <summary>An address a connection is refused at immediately: the discard port, on the loopback.</summary>
    public const string NoBrowser = "ws://127.0.0.1:9/";

    public static Dictionary<string, string> Variables(params (string Name, string Value)[] own)
    {
        var variables = new Dictionary<string, string>
        {
            [$"{EnvironmentSettings.Prefix}WEB__SCRAPING__BROWSER_SERVER"] = NoBrowser,
            [$"{EnvironmentSettings.Prefix}BROWSER_PROBE_SECONDS"] = "5"
        };

        foreach (var (name, value) in own)
            variables[$"{EnvironmentSettings.Prefix}{name}"] = value;

        return variables;
    }

    public static IConfiguration Settings(params (string Name, string Value)[] own) =>
        ServerConfigExtensions.Settings(Variables(own));
}
