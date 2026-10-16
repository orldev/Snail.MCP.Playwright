namespace Snail.MCP.Playwright.Application.Ports;

/// <summary>Whether something answers at the browser server's address, asked without renting a browser.</summary>
/// <remarks>
/// The pool answers the same question with a browser rented, and a rent waits for one of the few permits
/// that the page loads in flight hold: a server busy with four pages looked dead to the orchestrator's
/// health check for exactly as long as it was working. A knock takes no permit. Found by the audit of
/// 2026-10-16; the design is the owner's choice of 2026-10-17.
/// </remarks>
public interface IBrowserServer
{
    /// <summary>Whether the address answers within the patience: with any reply, or by closing the connection on the request; nobody accepting, or nobody reacting in time, is no.</summary>
    Task<bool> IsAnsweringAsync(TimeSpan patience, CancellationToken cancellationToken = default);
}
