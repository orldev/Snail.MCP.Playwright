using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.Toolkit.Web;

namespace Snail.MCP.Playwright.Tests.Chaos;

/// <summary>
/// Breaks the one rule that keeps a model from reading the deployment's own services: an inward address
/// written in a form the rule does not look at. A test that fails here is an address the browser would open.
/// </summary>
public sealed class AddressRulesChaosTests
{
    private static readonly AddressRules Rules = new(new ServerConfig());

    /// <summary>
    /// A trailing dot makes a host a name to the rule and a private address to the resolver; a decimal, hex or
    /// octal number is the same loopback the dotted form is; NAT64 and 6to4 prefixes carry a private IPv4 inside.
    /// </summary>
    [Theory]
    [InlineData("http://10.0.0.1./")]
    [InlineData("http://192.168.1.1./admin")]
    [InlineData("http://localhost./")]
    [InlineData("http://2130706433/")]
    [InlineData("http://0x7f000001/")]
    [InlineData("http://127.1/")]
    [InlineData("http://0177.0.0.1/")]
    [InlineData("http://[64:ff9b::7f00:1]/")]
    [InlineData("http://[2002:a00:1::]/")]
    [InlineData("http://[::ffff:127.0.0.1]/")]
    [InlineData("http://0.0.0.0/")]
    [InlineData("http://[::]/")]
    [InlineData("http://metadata.google.internal/")]
    [InlineData("http://169.254.169.254:80/")]
    public void Refusal_OfAnInwardAddressWrittenAnotherWay_StillRefuses(string url)
    {
        var link = Link.Of(url);

        Assert.NotNull(Rules.Refusal(link));
    }
}
