using System.Text.RegularExpressions;
using Snail.MCP.Playwright.Configuration;

namespace Snail.MCP.Playwright.Tests.Contracts;

/// <summary>
/// The files a deployment is made of say the same thing as the code: nothing compiles a Dockerfile or a
/// compose file against the server, so only a test notices when one of them moves alone.
/// </summary>
public sealed partial class DeploymentContractTests
{
    private const string Port = "8822";

    private static readonly string Dockerfile = Repository.Read("Dockerfile");

    private static readonly string Compose = Repository.Read("deploy", "compose.yml");

    /// <summary>On a floating tag a newer feature band refuses the build global.json pinned.</summary>
    [Fact]
    public void Image_IsBuiltWithTheSdk_TheRepositoryPins()
    {
        string pinned = JsonNode.Parse(Repository.Read("global.json"))!["sdk"]!["version"]!.GetValue<string>();

        Assert.Contains($"dotnet/sdk:{pinned} ", Dockerfile, StringComparison.Ordinal);
    }

    /// <summary>The platform computes the address from the registry entry; a port that differs between the files is a server nobody reaches.</summary>
    [Fact]
    public void Port_IsOneNumber_InEveryFileOfTheDeployment()
    {
        Assert.Contains($"SNAIL_MCP_PLAYWRIGHT_PORT={Port}", Dockerfile, StringComparison.Ordinal);
        Assert.Contains($"EXPOSE {Port}", Dockerfile, StringComparison.Ordinal);
        Assert.Contains($"mcp.port=\"{Port}\"", Dockerfile, StringComparison.Ordinal);
        Assert.Contains($"SNAIL_MCP_PLAYWRIGHT_PORT: {Port}", Compose, StringComparison.Ordinal);
        Assert.Contains($":{Port}\"", Compose, StringComparison.Ordinal);
    }

    [Fact]
    public void Labels_NameATool_ThatTellsALiveServerFromOneWithoutItsBrowser() =>
        Assert.Contains(Label("mcp.diagnose"), Catalog.Names);

    /// <summary>The identity the server announces describes one image; a deployment that runs another contradicts it in one line.</summary>
    [Fact]
    public void Browser_OfTheDeployment_IsTheImageTheDefaultsDescribe()
    {
        Assert.Contains($"image: {BrowserDefaults.Image}", Compose, StringComparison.Ordinal);
        Assert.Contains(BrowserDefaults.Image, Label("mcp.requires"), StringComparison.Ordinal);
    }

    /// <summary>A variable spelled wrongly is not refused by anything: it is simply never read.</summary>
    [Theory]
    [InlineData("Dockerfile")]
    [InlineData("deploy/compose.yml")]
    public void EveryVariable_ADeploymentSets_IsASettingTheServerReads(string file)
    {
        var variables = Variable().Matches(Repository.Read(file.Split('/'))).Select(match => match.Groups[1].Value).Distinct();

        Assert.All(variables, variable => Assert.True(IsRead(variable), variable));
    }

    private static bool IsRead(string variable) =>
        variable.StartsWith("WEB__", StringComparison.Ordinal)
        || typeof(ServerConfig).GetProperties().Any(setting =>
            setting.Name.Equals(EnvironmentSettings.ToConfigurationKey(variable), StringComparison.OrdinalIgnoreCase));

    private static string Label(string name) =>
        Regex.Match(Dockerfile, $"{Regex.Escape(name)}=\"([^\"]*)\"").Groups[1].Value;

    [GeneratedRegex(@"SNAIL_MCP_PLAYWRIGHT_([A-Z0-9_]+)", RegexOptions.CultureInvariant)]
    private static partial Regex Variable();
}
