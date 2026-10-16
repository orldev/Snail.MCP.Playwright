namespace Snail.MCP.Playwright.Application;

/// <summary>Which tools the request being served may use, read from the address it was posted to.</summary>
/// <remarks>
/// The platform gives a role a list of tools and appends it to the address as <c>&amp;tool=…</c>, once per
/// tool, on every request it posts. The model cannot reach the address, and the grant is checked where calls
/// arrive rather than inside each tool. A request that names no tool may use them all, which is what a
/// server started as a program for one caller has always been. There is no session to fix the grant to: the
/// server answers each request on its own, and the platform's worker writes the same address every time.
/// </remarks>
public sealed class RequestGrant(IRequestAddress address)
{
    /// <summary>What the address calls a tool it allows; repeat it for each one.</summary>
    public const string Parameter = "tool";

    /// <summary>The tools the request named, empty when it named none and may use everything.</summary>
    public IReadOnlyList<string> All =>
    [
        .. address.Values(Parameter)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
    ];

    /// <summary>Whether this request may call that tool.</summary>
    public bool Allows(string name)
    {
        var granted = All;

        return granted.Count == 0 || granted.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The refusal, in the words a caller needs to stop asking.</summary>
    public string Refusal(string name) =>
        $"'{name}' was not granted to this request, which may use {string.Join(", ", All)}";
}
