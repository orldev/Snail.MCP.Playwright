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
/// <para>
/// The host is read with its trailing dot removed and as an IP address wherever it parses as one, because
/// <c>10.0.0.1.</c> is a name to <see cref="Uri"/> and the private address to every resolver; found by the
/// audit of 2026-10-16, together with the NAT64 and 6to4 prefixes that carry an IPv4 address inside an IPv6 one.
/// </para>
/// </remarks>
public sealed class AddressRules(ServerConfig config)
{
    private static readonly string[] Inward = [".localhost", ".local", ".internal", ".lan", ".home.arpa"];

    /// <summary>Why this address will not be read, or null when it will.</summary>
    public AnswerFailure? Refusal(Link link)
    {
        if (link.Address is not { } address)
            return FailureTypes.Refused(Messages.NotAnAddress(link.Text));

        return !config.AllowPrivateAddresses && IsInward(address.Host)
            ? Answer.Failed(Messages.InwardAddress(address.Host), type: FailureTypes.PrivateAddress)
            : null;
    }

    private static bool IsInward(string host)
    {
        string named = Unbracketed(host).TrimEnd('.');

        if (IsLiteral(named) && IPAddress.TryParse(named, out var literal))
            return IsInward(literal);

        return !named.Contains('.', StringComparison.Ordinal)
               || named.Equals("localhost", StringComparison.OrdinalIgnoreCase)
               || EndsInward(named);
    }

    /// <summary>Whether the name ends in a suffix that never leaves the deployment.</summary>
    /// <remarks>A loop rather than <c>Any</c> with a lambda: the closure was allocated for every host, 88 bytes a call over the 256 links of the benchmark.</remarks>
    private static bool EndsInward(string host)
    {
        foreach (var suffix in Inward)
        {
            if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>The host without the brackets an IPv6 literal is written in.</summary>
    /// <remarks>Not <c>Trim('[', ']')</c>: its params array was allocated for every host, literal or not.</remarks>
    private static string Unbracketed(string host) =>
        host.StartsWith('[') && host.EndsWith(']') ? host[1..^1] : host;

    /// <summary>Whether the host can be an address at all: an IPv6 one has a colon, an IPv4 one opens with a digit.</summary>
    /// <remarks>
    /// A name is answered by its shape rather than by a parse that fails: measured on 2026-10-16, a public name
    /// costs the same 88 bytes a call it did before the literal forms were added, and a literal 8 bytes less.
    /// </remarks>
    private static bool IsLiteral(string host) =>
        host.Length > 0 && (host.Contains(':', StringComparison.Ordinal) || char.IsAsciiDigit(host[0]));

    private static bool IsInward(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            return IsInward(address.MapToIPv4());

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            return true;

        if (address.AddressFamily is AddressFamily.InterNetworkV6)
            return IsInwardV6(address);

        return IsInwardV4(address.GetAddressBytes());
    }

    private static bool IsInwardV6(IPAddress address)
    {
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal)
            return true;

        if (Embedded(address.GetAddressBytes()) is { } inside)
            return IsInwardV4(inside);

        return false;
    }

    /// <summary>The IPv4 address an IPv6 one carries: NAT64 (64:ff9b::/96) in its last four bytes, 6to4 (2002::/16) in the two after the prefix.</summary>
    private static byte[]? Embedded(byte[] bytes) => bytes switch
    {
        [0x00, 0x64, 0xff, 0x9b, 0, 0, 0, 0, 0, 0, 0, 0, .. var nat64] => nat64,
        [0x20, 0x02, .. var sixToFour] => sixToFour[..4],
        _ => null
    };

    private static bool IsInwardV4(byte[] bytes) =>
        bytes is [10, ..]
            or [172, >= 16 and <= 31, ..]
            or [192, 168, ..]
            or [169, 254, ..]
            or [100, >= 64 and <= 127, ..]
            or [127, ..]
            or [0, ..];
}
