using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using Snail.MCP.Playwright.Configuration;
using Snail.MCP.Playwright.Domain;
using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Toolkit.Web.Scraping;
using Snail.Toolkit.Web.Scraping.Agent;
using Snail.Toolkit.Web.Search;

namespace Snail.MCP.Playwright.Tests.Tools;

/// <summary>
/// Guard tests over what the model reads: a hardcoded description drifts from the code it describes
/// unless something fails when it does.
/// </summary>
public sealed partial class ToolCatalogTests
{
    [Fact]
    public void EveryTool_InTheAssembly_IsNamedAndDescribed()
    {
        Assert.NotEmpty(Catalog.Tools);
        Assert.All(Catalog.Tools, tool =>
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.GetCustomAttribute<McpServerToolAttribute>()!.Name));
            Assert.False(string.IsNullOrWhiteSpace(tool.GetCustomAttribute<DescriptionAttribute>()?.Description));
        });
    }

    /// <summary>A parameter the model cannot see the meaning of is one it fills in by guessing.</summary>
    /// <remarks>
    /// The cancellation token, the progress sink and the session are resolved by the SDK and never reach
    /// the schema the model reads; everything else is a question asked of the model.
    /// </remarks>
    [Fact]
    public void EveryParameter_TheModelFills_IsDescribed()
    {
        var parameters = Catalog.Tools
            .SelectMany(tool => tool.GetParameters())
            .Where(parameter => parameter.ParameterType != typeof(CancellationToken))
            .Where(parameter => parameter.ParameterType != typeof(McpServer))
            .Where(parameter => !parameter.ParameterType.IsGenericType || parameter.ParameterType.GetGenericTypeDefinition() != typeof(IProgress<>));

        Assert.All(parameters, parameter =>
            Assert.False(string.IsNullOrWhiteSpace(parameter.GetCustomAttribute<DescriptionAttribute>()?.Description), parameter.Name));
    }

    /// <summary>A client shows the tools of many servers in one list, and «search» there is a question without an answer.</summary>
    [Fact]
    public void EveryTool_OfThisServer_CarriesItsPrefix()
    {
        Assert.All(Catalog.Names, name => Assert.StartsWith("playwright_", name, StringComparison.Ordinal));
        Assert.Equal(Catalog.Names.Count, Catalog.Names.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// A tool without the hint is treated by the platform as one that writes, and every tool here only reads
    /// the web — but one. A walk presses whatever the form submits, and what that does on the site's side is
    /// not this server's to promise, so it alone is left as a tool a human is shown before it runs.
    /// </summary>
    [Fact]
    public void EveryTool_ButTheOneThatPressesButtons_IsDeclaredReadOnly() =>
        Assert.Equal([.. Catalog.Names.Except(["playwright_perform"])], [.. Catalog.Named(tool => tool.ReadOnly)]);

    /// <summary>A tool a text names and the server does not have is a call the model will make and lose.</summary>
    [Theory]
    [InlineData(typeof(ServerGuidance))]
    [InlineData(typeof(ToolDescriptions))]
    [InlineData(typeof(Messages))]
    public void EveryTool_ATextNames_Exists(Type texts)
    {
        var named = texts
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .SelectMany(field => ToolName().Matches((string)field.GetRawConstantValue()!))
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal);

        Assert.All(named, name => Assert.Contains(name, Catalog.Names));
    }

    /// <summary>
    /// The tool list is read into the context of every session before anything is asked, so its size is
    /// spent whether or not a tool is used. The platform measured 65 tools at 12 800 tokens — 39% of a
    /// 32k window — which is why this server is small on purpose and the budget says so out loud. Measured on
    /// 2026-10-04 at 5661 characters over eight tools, which a client receives as a list of 8.5 KB; the ninth,
    /// playwright_extract_fields, took it to 6142 on 2026-10-10 only after the extraction texts were cut by half,
    /// so the next tool, or the next paragraph, has to take something out first.
    /// </summary>
    [Fact]
    public void TheToolList_ReadIntoEverySession_StaysWithinItsBudget()
    {
        int characters = Catalog.Tools.Sum(tool =>
            tool.GetCustomAttribute<McpServerToolAttribute>()!.Name!.Length
            + tool.GetCustomAttribute<DescriptionAttribute>()!.Description.Length
            + tool.GetParameters().Sum(parameter => parameter.Name!.Length + (parameter.GetCustomAttribute<DescriptionAttribute>()?.Description.Length ?? 0)));

        Assert.InRange(characters, 1, 6144);
    }

    /// <summary>A limit a caller is not told about is one it meets as a refusal in the middle of its work.</summary>
    [Fact]
    public void SearchDescription_StatesTheLimitsTheCodeHolds()
    {
        Assert.Contains($"1 to {Limits.MostQueries} search queries", ToolDescriptions.SearchQueries, StringComparison.Ordinal);
        Assert.Contains($"up to {Limits.MostQueryCharacters} characters", ToolDescriptions.SearchQueries, StringComparison.Ordinal);
        Assert.Contains($"1 to {Limits.MostResults}; {Limits.DefaultResults} by default", ToolDescriptions.SearchLimit, StringComparison.Ordinal);
    }

    [Fact]
    public void FetchDescription_StatesTheLimitsTheCodeHolds()
    {
        Assert.Contains(
            $"{Limits.FewestCharacters} to {Limits.MostCharacters}; {Limits.DefaultCharacters} by default",
            ToolDescriptions.FetchCharacters,
            StringComparison.Ordinal);

        Assert.Contains($"within {new ServerConfig().PageMemoryMinutes} minutes", ToolDescriptions.Fetch, StringComparison.Ordinal);
    }

    [Fact]
    public void FetchManyDescription_StatesTheLimitsTheCodeHolds()
    {
        Assert.Contains($"1 to {Limits.MostAddresses} absolute", ToolDescriptions.FetchManyUrls, StringComparison.Ordinal);
        Assert.Contains(
            $"{Limits.FewestCharacters} to {Limits.MostCharactersEach}; {Limits.DefaultCharactersEach} by default",
            ToolDescriptions.FetchManyCharacters,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FetchSiteDescription_StatesTheLimitsTheCodeHolds()
    {
        Assert.Contains($"1 to {Limits.MostSitePages}; {Limits.DefaultSitePages} by default", ToolDescriptions.FetchSitePages, StringComparison.Ordinal);
        Assert.Contains($"0 to {Limits.MostSiteDepth}; {Limits.DefaultSiteDepth} by default", ToolDescriptions.FetchSiteDepth, StringComparison.Ordinal);
        Assert.Contains(
            $"{Limits.FewestSiteSeconds} to {Limits.MostSiteSeconds}; {Limits.DefaultSiteSeconds} by default",
            ToolDescriptions.FetchSiteSeconds,
            StringComparison.Ordinal);
        Assert.Contains(
            $"{Limits.FewestCharacters} to {Limits.MostCharactersEach}; {Limits.DefaultSiteCharactersEach} by default",
            ToolDescriptions.FetchSiteCharacters,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractFieldsDescription_StatesTheLimitsTheCodeHolds()
    {
        Assert.Contains($"1 to {Limits.MostFields} fields", ToolDescriptions.ExtractFieldsFields, StringComparison.Ordinal);
        Assert.Contains($"1 to {Limits.MostCandidates} selectors a field", ToolDescriptions.ExtractFieldsFields, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractDescription_StatesTheLimitsTheCodeHolds()
    {
        Assert.Contains($"1 to {Limits.MostSelected}; {Limits.DefaultSelected} by default", ToolDescriptions.ExtractLimit, StringComparison.Ordinal);
        Assert.Contains($"within {new ServerConfig().PageMemoryMinutes} minutes", ToolDescriptions.Extract, StringComparison.Ordinal);
    }

    [Fact]
    public void ScreenshotDescription_StatesTheCeilingTheToolkitHolds() =>
        Assert.Contains($"up to {PageSnapshots.TallestPage} pixels", ToolDescriptions.ScreenshotWholePage, StringComparison.Ordinal);

    /// <summary>A move or a reaction the toolkit knows and the description never names is one no model will ever write.</summary>
    [Fact]
    public void PerformDescription_NamesEveryMoveAndEveryReaction_AndTheLimitsTheCodeHolds()
    {
        Assert.All(Enum.GetNames<StepKind>(), move => Assert.Contains(move.ToLowerInvariant(), ToolDescriptions.PerformMoves, StringComparison.Ordinal));
        Assert.All(Enum.GetNames<EffectKind>(), reaction => Assert.Contains(reaction.ToLowerInvariant(), ToolDescriptions.PerformMoves, StringComparison.Ordinal));
        Assert.Contains($"1 to {Limits.MostMoves} moves", ToolDescriptions.PerformMoves, StringComparison.Ordinal);
        Assert.Contains($"1 to {Limits.MostWithinSeconds}; {Limits.DefaultWithinSeconds} by default", ToolDescriptions.PerformMoves, StringComparison.Ordinal);
        Assert.Contains(
            $"{Limits.FewestCharacters} to {Limits.MostCharacters}; {Limits.DefaultCharacters} by default",
            ToolDescriptions.PerformCharacters,
            StringComparison.Ordinal);
    }

    /// <summary>The description tells a model to read the stop; a stop it was never told the meaning of is one it cannot act on.</summary>
    [Fact]
    public void EveryWayAWalkCanEnd_SaysWhatItMeans() =>
        Assert.Equal(Enum.GetValues<CrawlStop>().Order(), Messages.CrawlStops.Keys.Order());

    /// <summary>The toolkit growing a failure kind has to fail here until this server says what that kind calls for.</summary>
    [Fact]
    public void EveryWayAPageReadCanFail_HasItsAdvice() =>
        Assert.Equal(Enum.GetValues<FailureKind>().Order(), Messages.PageHints.Keys.Order());

    [Fact]
    public void EveryWayAnEngineCanFail_HasItsAdvice() =>
        Assert.Equal(Enum.GetValues<SearchFailureKind>().Order(), Messages.SearchHints.Keys.Order());

    [GeneratedRegex(@"playwright_[a-z_]+", RegexOptions.CultureInvariant)]
    private static partial Regex ToolName();
}
