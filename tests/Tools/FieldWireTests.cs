using System.Text.Json;
using ModelContextProtocol;
using Snail.MCP.Playwright.Domain;

namespace Snail.MCP.Playwright.Tests.Tools;

/// <summary>A field arrives as JSON the model wrote: its names on the wire are the contract, whatever the record calls them.</summary>
public sealed class FieldWireTests
{
    [Fact]
    public void Field_ManyOnTheWire_WantsEveryMatch()
    {
        var field = JsonSerializer.Deserialize<Field>("""{"name":"cells","xpath":["//td"],"many":true}""", McpJsonUtilities.DefaultOptions);

        Assert.True(field!.IsEveryMatchWanted);
    }
}
