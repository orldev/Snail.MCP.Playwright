namespace Snail.MCP.Playwright.Domain;

/// <summary>How a caller names elements of a page: a CSS selector or an XPath expression.</summary>
/// <remarks>
/// Which of the two is said, never inferred from the shape of the string: <c>a.price</c> and <c>//a</c> look
/// different to a person, but «this starts with a slash» is a guess, and a guess here turns into a parse error
/// the caller cannot explain.
/// </remarks>
public sealed record Expression(string Text, ExpressionKind Kind)
{
    /// <summary>A CSS selector.</summary>
    public static Expression Css(string text) => new(text, ExpressionKind.Css);

    /// <summary>An XPath 1.0 expression.</summary>
    public static Expression XPath(string text) => new(text, ExpressionKind.XPath);
}

/// <summary>The language an expression is written in.</summary>
public enum ExpressionKind
{
    /// <summary>A CSS selector: <c>table#prices tr</c>, <c>a.title</c>.</summary>
    Css,

    /// <summary>An XPath 1.0 expression: <c>//table[@id='prices']//tr</c>.</summary>
    XPath
}
