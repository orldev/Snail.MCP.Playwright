using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web;
using Snail.Toolkit.Web.Scraping;

namespace Snail.MCP.Playwright.Application;

/// <summary>Shows a page instead of reading it: a picture of what the browser drew.</summary>
/// <remarks>
/// For what text cannot carry — a chart, a map, a layout, a page whose content is drawn rather than
/// written. It is a visit like the reader's, so a site that would refuse the text refuses the picture.
/// </remarks>
public sealed class PagePictures(IPageSnapshots snapshots, AddressRules rules)
{
    /// <summary>A picture of the screen a visitor sees, or of the whole page.</summary>
    public async Task<Answer> TakeAsync(string url, bool wholePage, CancellationToken cancellationToken = default)
    {
        var address = Link.Of(url ?? string.Empty);

        if (rules.Refusal(address) is { } refusal)
            return Answer.Failed(refusal);

        var outcome = await snapshots.TakeAsync(address, wholePage ? SnapshotFrame.WholePage : SnapshotFrame.Screen, cancellationToken);

        return outcome.TryGetValue(out var snapshot)
            ? Answer.Ok(Described(snapshot, wholePage)) with { Picture = new Picture(snapshot.Image, PageSnapshot.MediaType) }
            : PageReading.Refused(outcome.Failure!);
    }

    private static JsonObject Described(PageSnapshot snapshot, bool wholePage) => new()
    {
        ["url"] = snapshot.Final.Text,
        ["requested"] = snapshot.Final != snapshot.Requested ? snapshot.Requested.Text : null,
        ["title"] = snapshot.Title,
        ["status"] = snapshot.StatusCode,
        ["takenAt"] = snapshot.TakenAt.ToString("O"),
        ["width"] = snapshot.Width,
        ["height"] = snapshot.Height,
        ["bytes"] = snapshot.Image.Length,
        ["isCut"] = snapshot.IsCut,
        ["note"] = snapshot.IsCut ? Messages.PictureCut(wholePage) : null
    };
}
