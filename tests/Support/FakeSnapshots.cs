using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Tests.Support;

/// <summary>A camera that hands back the picture a test gave it, and remembers the frame it was asked for.</summary>
internal sealed class FakeSnapshots(Outcome<PageSnapshot, PageFailure>? outcome = null) : IPageSnapshots
{
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    public SnapshotFrame? Framed { get; private set; }

    public Task<Outcome<PageSnapshot, PageFailure>> TakeAsync(Link address, CancellationToken ct = default) =>
        TakeAsync(address, SnapshotFrame.Screen, ct);

    public Task<Outcome<PageSnapshot, PageFailure>> TakeAsync(Link address, SnapshotFrame frame, CancellationToken ct = default)
    {
        Framed = frame;

        return Task.FromResult(outcome ?? Outcome<PageSnapshot, PageFailure>.Success(
            new PageSnapshot(address, Jpeg, DateTimeOffset.UnixEpoch) { Title = "A page", StatusCode = 200, Width = 1280, Height = 720, IsCut = true }));
    }
}
