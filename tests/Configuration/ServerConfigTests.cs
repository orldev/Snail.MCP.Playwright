using Microsoft.Extensions.Configuration;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Domain;
using Snail.MCP.Playwright.Extensions;
using Snail.Sdk.Mcp.Hosting;

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
        var settings = ServerConfigExtensions.Settings(null, new Dictionary<string, string> { ["PORT"] = "1", ["SNAIL_MCP_GIT_PORT"] = "2" });

        Assert.Equal(McpTransport.Stdio, McpServerHost.EndpointOf(settings).Transport);
    }

    [Fact]
    public void Settings_WithNothingConfigured_DescribeTheBundledBrowser()
    {
        var settings = ServerConfigExtensions.Settings(null, new Dictionary<string, string>());

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

    /// <summary>The settings file holds the server's settings and the toolkit's alike, over the browser defaults.</summary>
    [Fact]
    public void Settings_FromASettingsFile_ReachTheServerAndTheToolkit()
    {
        var folder = new DirectoryInfo(TestFolder.Create());

        try
        {
            string file = Path.Combine(folder.FullName, "config.json");
            File.WriteAllText(file, """{ "Engines": "Bing", "Web": { "Scraping": { "BrowserServer": "ws://browser:3000/" } } }""");

            var settings = ServerConfigExtensions.Settings(file, new Dictionary<string, string>());

            Assert.Equal("ws://browser:3000/", settings["Web:Scraping:BrowserServer"]);
            Assert.Equal([SearchEngines.Bing], SearchEngines.Named(ServerConfigExtensions.Read(settings).Engines));
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    /// <summary>A variable is what a deployment says for this one run, so it overrules the file a person keeps.</summary>
    [Fact]
    public void Settings_InTheFileAndInAVariable_HearTheVariable()
    {
        var folder = new DirectoryInfo(TestFolder.Create());

        try
        {
            string file = Path.Combine(folder.FullName, "config.json");
            File.WriteAllText(file, """{ "Engines": "Bing", "PageMemoryPages": 16 }""");

            var config = ServerConfigExtensions.Read(ServerConfigExtensions.Settings(file, Deployment.Variables(("ENGINES", "Brave"))));

            Assert.Equal([SearchEngines.Brave], SearchEngines.Named(config.Engines));
            Assert.Equal(16, config.PageMemoryPages);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public void Config_WithNothingConfigured_SpeaksStdioAndAsksTheDefaultEngines()
    {
        var settings = ServerConfigExtensions.Settings(null, new Dictionary<string, string>());
        var config = ServerConfigExtensions.Read(settings);

        Assert.Equal(McpTransport.Stdio, McpServerHost.EndpointOf(settings).Transport);
        Assert.Equal([SearchEngines.DuckDuckGoBrowser, SearchEngines.Bing, SearchEngines.Brave], SearchEngines.Named(config.Engines));
    }

    [Fact]
    public void Engines_WrittenInAnyCase_AreSpelledTheWayTheToolkitSpellsThem() =>
        Assert.Equal([SearchEngines.Google, SearchEngines.DuckDuckGo], SearchEngines.Named(" google, DUCKDUCKGO ,google"));

    [Theory]
    [InlineData("BROWSER_PROBE_SECONDS", "0", "SNAIL_MCP_PLAYWRIGHT_BROWSER_PROBE_SECONDS")]
    [InlineData("ENGINES", "Yandex", "Yandex")]
    [InlineData("ENGINES", " , ", "SNAIL_MCP_PLAYWRIGHT_ENGINES")]
    public void Config_WithASettingOutOfRange_StopsTheStartAndNamesIt(string variable, string value, string named)
    {
        var refused = Assert.Throws<StartRefusedException>(() => ServerConfigExtensions.Read(Deployment.Settings((variable, value))));

        Assert.Contains(named, refused.Message, StringComparison.Ordinal);
    }

    /// <summary>A port alone means HTTP on it, on loopback outside a container: the image's port, the developer's run.</summary>
    [Fact]
    public void Endpoint_WithAPort_IsHttpOnThatPortOfLoopback()
    {
        var endpoint = McpServerHost.EndpointOf(Deployment.Settings(("PORT", "8822")));

        Assert.Equal(McpTransport.Http, endpoint.Transport);
        Assert.Equal(8822, endpoint.Url.Port);
        Assert.True(endpoint.Url.IsLoopback);
    }

    /// <summary>The operators of this server wrote its settings under <c>Snail</c>, the endpoint's among them.</summary>
    [Fact]
    public void Endpoint_WithAPortUnderTheSnailSection_IsHttpOnThatPort()
    {
        var endpoint = McpServerHost.EndpointOf(Deployment.Settings(("SNAIL__PORT", "8823")));

        Assert.Equal(McpTransport.Http, endpoint.Transport);
        Assert.Equal(8823, endpoint.Url.Port);
    }

    /// <summary>The server has no token: outside its image the network is not opened by one setting.</summary>
    /// <remarks>The endpoint is read under the server's name, so the refusal names the variable in the test's process too, where the entry assembly is the test runner.</remarks>
    [OutsideAContainerFact]
    public void Endpoint_OnEveryInterfaceOutsideAContainer_StopsTheStartAndNamesTheVariable()
    {
        var refused = Assert.Throws<StartRefusedException>(() =>
            McpServerHost.EndpointOf(Deployment.Settings(("PORT", "8822"), ("LISTENS_ON_EVERY_INTERFACE", "true"))));

        Assert.Contains("SNAIL_MCP_PLAYWRIGHT_LISTENS_ON_EVERY_INTERFACE", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Endpoint_WithAPortOutOfRange_StopsTheStartAndNamesIt()
    {
        var refused = Assert.Throws<StartRefusedException>(() => McpServerHost.EndpointOf(Deployment.Settings(("PORT", "70000"))));

        Assert.Contains("SNAIL_MCP_PLAYWRIGHT_PORT", refused.Message, StringComparison.Ordinal);
    }
}
