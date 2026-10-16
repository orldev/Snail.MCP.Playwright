using Microsoft.AspNetCore.Http;

namespace Snail.MCP.Playwright.Adapters;

/// <summary>The address the call was posted to, read from the request being served.</summary>
/// <remarks>
/// The server answers each request on its own, and the platform's worker posts every call to the same
/// URL, so the query of the current request is the whole of what the caller said about it.
/// </remarks>
public sealed class HttpAddress(IHttpContextAccessor requests) : IRequestAddress
{
    /// <inheritdoc/>
    public IReadOnlyList<string> Values(string parameter) =>
        requests.HttpContext is { } request
            ? [.. request.Request.Query[parameter].Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!)]
            : [];
}
