using Microsoft.Extensions.Options;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Adapters;

/// <summary>The browser server knocked at over plain HTTP: the ws:// address it is configured at, asked as http://.</summary>
/// <remarks>
/// A Playwright browser server speaks WebSocket, and a plain request is not what it is for; what the knock
/// asks is only whether a process stands there and reacts. Any HTTP reply counts, and so does a connection
/// the server closes on seeing the request — both take a process. A refused connection, a name that does
/// not resolve or silence until the patience runs out is what a container being restarted and a dead proxy
/// give, and those are the states a health check exists to report.
/// </remarks>
public sealed class HttpBrowserServer(IOptions<ScrapingOptions> scraping) : IBrowserServer, IDisposable
{
    private readonly HttpClient _http = new(new SocketsHttpHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };

    /// <inheritdoc/>
    public async Task<bool> IsAnsweringAsync(TimeSpan patience, CancellationToken cancellationToken = default)
    {
        if (Knockable(scraping.Value.BrowserServer) is not { } address)
            return false;

        using var patient = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        patient.CancelAfter(patience);

        try
        {
            using var knock = new HttpRequestMessage(HttpMethod.Get, address);
            using var reply = await _http.SendAsync(knock, HttpCompletionOption.ResponseHeadersRead, patient.Token);

            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (HttpRequestException refused)
        {
            return IsSomebodyThere(refused.HttpRequestError);
        }
    }

    /// <summary>The address to knock at: the browser server's, with http for ws and https for wss; null when none is configured.</summary>
    internal static Uri? Knockable(string? browserServer)
    {
        if (!Uri.TryCreate(browserServer, UriKind.Absolute, out var configured))
            return null;

        bool isSecure = configured.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) || configured.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        var knockable = new UriBuilder(configured) { Scheme = isSecure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp };

        return knockable.Uri;
    }

    /// <summary>Whether the way the request failed still proves a process at the address: anything but nobody accepting and nobody to resolve.</summary>
    private static bool IsSomebodyThere(HttpRequestError error) =>
        error is not HttpRequestError.ConnectionError and not HttpRequestError.NameResolutionError;

    /// <inheritdoc/>
    public void Dispose() => _http.Dispose();
}
