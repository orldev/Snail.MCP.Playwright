namespace Snail.MCP.Playwright.Application;

/// <summary>A batch read: a piece of every page that answered and the reason for every one that did not, in the order asked.</summary>
/// <param name="Asked">How many distinct addresses were asked for.</param>
/// <param name="Pages">A piece of each page that was read.</param>
/// <param name="Unread">Each address that was not read, and why.</param>
public sealed record PagesRead(int Asked, IReadOnlyList<PagePiece> Pages, IReadOnlyList<UnreadPage> Unread);
