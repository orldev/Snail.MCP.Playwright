using Microsoft.Extensions.Configuration;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Domain;

namespace Snail.MCP.Playwright.Tests.Configuration;

public sealed class ServerConfigTests
{
    [Theory]
    [InlineData("PORT", "Port")]
    [InlineData("BROWSER_PROBE_SECONDS", "BrowserProbeSeconds")]
    [InlineData("WEB__SCRAPING__BROWSER_SERVER", "Web:Scraping:BrowserServer")]
    [InlineData("WEB__IDENTITY__FINGERPRINTS__0__USER_AGENT", "Web:Identity:Fingerprints:0:UserAgent")]
    public void Variable_UnderThePrefix_BecomesTheKeyTheCodeBinds(string variable, string key) =>
        Assert.Equal(key, EnvironmentSettings.ToConfigurationKey(variable), ignoreCase: true);

    [Fact]
    public void Variable_OfAnotherProgram_IsNotASetting()
    {
        var read = EnvironmentSettings.Read(new Dictionary<string, string> { ["PORT"] = "1", ["SNAIL_MCP_GIT_PORT"] = "2" });

        Assert.Empty(read);
    }

    [Fact]
    public void Settings_WithNothingConfigured_DescribeTheBundledBrowser()
    {
        var settings = ServerConfigExtensions.Settings(new Dictionary<string, string>());

        Assert.Equal(BrowserDefaults.Server, settings["Web:Scraping:BrowserServer"]);
        Assert.Equal("Firefox", settings["Web:Scraping:Engine"]);
        Assert.Contains($"Firefox/{BrowserDefaults.FirefoxVersion}", settings["Web:Identity:Fingerprints:0:UserAgent"], StringComparison.Ordinal);
    }

    /// <summary>The defaults sit under the environment, not over it: a deployment naming its own browser is heard.</summary>
    [Fact]
    public void Settings_WithABrowserConfigured_PreferItToTheDefault()
    {
        var settings = Deployment.Settings(("WEB__SCRAPING__BROWSER_SERVER", "ws://browser:3000/"));

        Assert.Equal("ws://browser:3000/", settings["Web:Scraping:BrowserServer"]);
    }

    [Fact]
    public void Config_WithNothingConfigured_SpeaksStdioAndAsksTheDefaultEngines()
    {
        var config = ServerConfigExtensions.Read(ServerConfigExtensions.Settings(new Dictionary<string, string>()));

        Assert.Equal(0, config.Port);
        Assert.Equal([SearchEngines.DuckDuckGo, SearchEngines.Bing, SearchEngines.Brave], SearchEngines.Named(config.Engines));
    }

    [Fact]
    public void Engines_WrittenInAnyCase_AreSpelledTheWayTheToolkitSpellsThem() =>
        Assert.Equal([SearchEngines.Google, SearchEngines.DuckDuckGo], SearchEngines.Named(" google, DUCKDUCKGO ,google"));

    [Theory]
    [InlineData("PORT", "70000", "Port")]
    [InlineData("BROWSER_PROBE_SECONDS", "0", "BrowserProbeSeconds")]
    [InlineData("ENGINES", "Yandex", "Yandex")]
    [InlineData("ENGINES", " , ", "Engines")]
    public void Config_WithASettingOutOfRange_StopsTheStartAndNamesIt(string variable, string value, string named)
    {
        var refused = Assert.Throws<InvalidOperationException>(() => ServerConfigExtensions.Read(Deployment.Settings((variable, value))));

        Assert.Contains(named, refused.Message, StringComparison.Ordinal);
    }
}
