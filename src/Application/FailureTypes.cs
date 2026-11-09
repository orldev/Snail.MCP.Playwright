using Snail.Sdk.Mcp.Hosting;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>The words a refusal of this server carries as its <c>type</c>, the one a program branches on instead of reading the sentence.</summary>
/// <remarks>
/// Snail.Sdk's word where one fits, used by name: <see cref="ToolFailures.Arguments"/> for a call that cannot be served as written. A page
/// that failed is typed by the toolkit's kind, in camel case — <c>blocked</c>, <c>robotsDisallowed</c>, <c>timeout</c> — and keeps that kind
/// as the toolkit spells it in <c>data.kind</c>, which clients read before the type existed.
/// </remarks>
public static class FailureTypes
{
    /// <summary>An address behind the public web — the loopback, a private range, a name with no dot in it — which this server does not read.</summary>
    public const string PrivateAddress = "privateAddress";

    /// <summary>No search engine answered any query; each engine's own failure is in <c>data.engines</c>.</summary>
    public const string Unanswered = "unanswered";

    /// <summary>Not one address of a batch was read; each address's own failure is in <c>data.failed</c>.</summary>
    public const string NothingRead = "nothingRead";

    /// <summary>The type of a page that failed: the toolkit's kind in camel case.</summary>
    public static string Of(FailureKind kind) => ToolAnswer.Word(kind, WordCase.Camel);

    /// <summary>A refusal of the call's arguments, with nothing yet sent to any site.</summary>
    public static AnswerFailure Refused(string error) => Answer.Failed(error, type: ToolFailures.Arguments);
}
