namespace Snail.MCP.Playwright.Domain;

/// <summary>What a tool has to say: the payload, or the reason there is none.</summary>
/// <remarks>
/// Failure travels as data all the way to the tool boundary, so that a page that refused, an engine that
/// is resting and a browser that is not running all reach the model as something it can act on rather
/// than as a stack trace it cannot.
/// </remarks>
public sealed record Answer(JsonNode? Data, string? Error, string? Hint)
{
    /// <summary>The image that goes with the payload, when the answer is something to look at.</summary>
    public Picture? Picture { get; init; }

    /// <summary>Whether there is a payload to read rather than a reason to act on.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>An answer that carries what was asked for.</summary>
    public static Answer Ok(JsonNode data) => new(data, null, null);

    /// <summary>An answer that says why there is nothing, and what to do about it.</summary>
    public static Answer Failed(string error, string? hint = null) => new(null, error, hint);

    /// <summary>A failure that still has something to report: what was learned before it.</summary>
    public static Answer Stopped(JsonNode data, string error, string? hint = null) => new(data, error, hint);
}
