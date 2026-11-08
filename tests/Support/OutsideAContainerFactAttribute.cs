namespace Snail.MCP.Playwright.Tests.Support;

/// <summary>A test of what the server refuses outside a container, skipped inside one, where the same setting is allowed.</summary>
/// <remarks>Snail.Sdk reads <c>DOTNET_RUNNING_IN_CONTAINER</c> from the process itself, so a test cannot pretend either way.</remarks>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class OutsideAContainerFactAttribute : FactAttribute
{
    public OutsideAContainerFactAttribute()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
            Skip = "inside a container every interface is this server's to listen on";
    }
}
