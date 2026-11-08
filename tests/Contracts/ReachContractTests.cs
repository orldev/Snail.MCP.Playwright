namespace Snail.MCP.Playwright.Tests.Contracts;

/// <summary>How far the program may listen, decided before it opens a port: the server has no token, so outside its image it keeps to loopback.</summary>
public sealed class ReachContractTests
{
    /// <summary>An operator who sets the variable is told in one line which one stopped the start, with exit code 1 and no trace, rather than served to the network.</summary>
    [OutsideAContainerFact]
    public async Task Program_OnEveryInterfaceOutsideAContainer_ExitsNamingTheVariable()
    {
        var environment = Deployment.Variables(("LISTENS_ON_EVERY_INTERFACE", "true"))
            .ToDictionary(variable => variable.Key, variable => (string?)variable.Value);

        var refused = await Assert.ThrowsAsync<ServerExitedException>(() =>
            HttpServerProgram.StartAsync("Snail.MCP.Playwright", "SNAIL_PLAYWRIGHT_SERVER", environment, readiness: null));

        Assert.Equal(1, refused.ExitCode);
        Assert.Single(refused.Stderr.Split('\n'));
        Assert.Contains("SNAIL_MCP_PLAYWRIGHT_LISTENS_ON_EVERY_INTERFACE", refused.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", refused.Stderr, StringComparison.Ordinal);
    }
}
