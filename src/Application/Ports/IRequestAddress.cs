namespace Snail.MCP.Playwright.Application.Ports;

/// <summary>What the address of the request being served says.</summary>
/// <remarks>
/// A server reached over HTTP is posted to at a URL, and the URL is the one place a caller can state
/// something about the call that the model did not write. The platform writes it and the model cannot
/// reach it, which is why the grant travels there: <c>…/mcp?tool=playwright_search</c>. A server speaking
/// stdio has no address and answers nothing here.
/// </remarks>
public interface IRequestAddress
{
    /// <summary>Every value the address gives that parameter, empty when there is no address at all.</summary>
    IReadOnlyList<string> Values(string parameter);
}
