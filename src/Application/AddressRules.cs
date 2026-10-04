using System.Net;
using System.Net.Sockets;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web;

namespace Snail.MCP.Playwright.Application;

/// <summary>Which addresses this server will read: the public web, over http and https, and nothing behind it.</summary>
/// <remarks>
/// An address is judged by what it says, before anything is sent: a literal address in a private range,
/// the loopback, or a name with no dot in it — which is how one container calls another. A public name
/// that resolves to a private address is NOT caught here; that takes the resolver, and the deployment
/// answers it instead by keeping the browser in a network of its own.
/// </remarks>
public sealed class AddressRules(ServerConfig config)
{
    private static readonly string[] Inward = [".localhost", ".local", ".internal", ".lan", ".home.arpa"];

    /// <summary>Why this address will not be read, or null when it will.</summary>
    public string? Refusal(Link link)
    {
        if (link.Address is not { } address)
            return Messages.NotAnAddress(link.Text);

        return !config.AllowPrivateAddresses && IsInward(address) ? Messages.InwardAddress(address.Host) : null;
    }

    private static bool IsInward(Uri address) =>
        address.HostNameType switch
        {
            UriHostNameType.IPv4 or UriHostNameType.IPv6 => IsInward(IPAddress.Parse(address.Host.Trim('[', ']'))),
            _ => IsInward(address.Host)
        };

    private static bool IsInward(string host) =>
        !host.Contains('.', StringComparison.Ordinal)
        || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || Inward.Any(suffix => host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

    private static bool IsInward(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            return IsInward(address.MapToIPv4());

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            return true;

        if (address.AddressFamily is AddressFamily.InterNetworkV6)
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;

        return address.GetAddressBytes() is [10, ..]
            or [172, >= 16 and <= 31, ..]
            or [192, 168, ..]
            or [169, 254, ..]
            or [100, >= 64 and <= 127, ..]
            or [0, ..];
    }
}
