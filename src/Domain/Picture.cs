namespace Snail.MCP.Playwright.Domain;

/// <summary>An image that travels with an answer, beside its words rather than inside them.</summary>
public sealed record Picture(ReadOnlyMemory<byte> Bytes, string MediaType);
