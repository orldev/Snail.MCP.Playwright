using System.Text.RegularExpressions;
using System.Xml.Linq;
using Snail.MCP.Playwright.Configuration;

namespace Snail.MCP.Playwright.Tests.Contracts;

/// <summary>
/// The manifest a registry shows, the entry the platform's catalog takes and the settings the code binds
/// say the same thing. All three are written by hand, and none of them is compiled against the others.
/// </summary>
public sealed partial class RegistryManifestContractTests
{
    private static readonly JsonNode Manifest = JsonNode.Parse(Repository.Read(".mcp", "server.json"))!;

    private static readonly JsonNode Entry = JsonNode.Parse(Repository.Read("deploy", "swarm", "catalog-entry.json"))!;

    private static IEnumerable<JsonNode> Variables => Manifest["packages"]![0]!["environmentVariables"]!.AsArray()!;

    /// <summary>A setting the code reads and the manifest never names is one no operator will find.</summary>
    [Fact]
    public void Manifest_NamesEverySettingOfTheServer_WithTheDefaultTheCodeStartsFrom()
    {
        var config = new ServerConfig();

        Assert.All(typeof(ServerConfig).GetProperties(), setting =>
        {
            string variable = $"{EnvironmentSettings.Prefix}{Screaming().Replace(setting.Name, "_$1").ToUpperInvariant()}";
            var named = Variables.SingleOrDefault(candidate => candidate["name"]!.GetValue<string>() == variable);

            Assert.True(named is not null, variable);
            Assert.Equal(Convert.ToString(setting.GetValue(config), System.Globalization.CultureInfo.InvariantCulture), named["default"]!.GetValue<string>(), ignoreCase: true);
        });
    }

    /// <summary>The defaults the manifest states for the browser are the ones laid under every deployment.</summary>
    [Fact]
    public void Manifest_StatesTheBrowserDefaults_TheServerIsStartedWith()
    {
        var settings = ServerConfigExtensions.Settings(new Dictionary<string, string>());

        var stated = Variables
            .Select(variable => (Key: EnvironmentSettings.ToConfigurationKey(variable["name"]!.GetValue<string>()[EnvironmentSettings.Prefix.Length..]), Default: variable["default"]?.GetValue<string>()))
            .Where(variable => settings[variable.Key] is not null);

        Assert.All(stated, variable => Assert.Equal(settings[variable.Key], variable.Default));
        Assert.Equal(BrowserDefaults.Settings.Count, stated.Count());
    }

    [Fact]
    public void Manifest_CarriesTheVersion_OfThePackageItDescribes()
    {
        string version = XDocument.Parse(Repository.Read("src", "Snail.MCP.Playwright.csproj")).Descendants("Version").Single().Value;

        Assert.Equal(version, Manifest["version"]!.GetValue<string>());
        Assert.Equal(version, Manifest["packages"]![0]!["version"]!.GetValue<string>());
        Assert.EndsWith($":{version}", Entry["image"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>The platform computes the address from this entry; a port or a tool that differs from the image is a server nobody reaches.</summary>
    [Fact]
    public void CatalogEntry_AgreesWithTheImage_OnThePortThePathAndTheToolThatDiagnoses()
    {
        string dockerfile = Repository.Read("Dockerfile");

        Assert.Contains($"mcp.port=\"{Entry["port"]!.GetValue<int>()}\"", dockerfile, StringComparison.Ordinal);
        Assert.Contains($"mcp.path=\"{Entry["path"]!.GetValue<string>()}\"", dockerfile, StringComparison.Ordinal);
        Assert.Contains(Entry["diagnoseTool"]!.GetValue<string>(), Catalog.Names);
        Assert.Contains(BrowserDefaults.Image, Entry["requires"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>The compose block for the platform and the entry's environment have to name one browser, at one address.</summary>
    [Fact]
    public void CatalogEntry_AndThePlatformsComposeBlock_PointTheServerAtTheSameBrowser()
    {
        string compose = Repository.Read("deploy", "swarm", "compose.yml");
        string address = Entry["env"]![$"{EnvironmentSettings.Prefix}WEB__SCRAPING__BROWSER_SERVER"]!.GetValue<string>();

        Assert.Contains($"{EnvironmentSettings.Prefix}WEB__SCRAPING__BROWSER_SERVER: {address}", compose, StringComparison.Ordinal);
        Assert.Contains($"  {new Uri(address).Host}:", compose, StringComparison.Ordinal);
        Assert.Contains($"image: {BrowserDefaults.Image}", compose, StringComparison.Ordinal);
        Assert.Contains("  mcp-playwright:", compose, StringComparison.Ordinal);
    }

    [GeneratedRegex("(?<!^)([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex Screaming();
}
