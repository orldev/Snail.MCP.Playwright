using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Domain;

namespace Snail.MCP.Playwright.Tests.Chaos;

/// <summary>
/// Breaks the validation of a move: <c>Enum.TryParse</c> accepts a comma-separated list for any enum and ORs the
/// values, so two move names in one word parse as a third move. A caller writing «pick,click» would be told it
/// is not a move; instead the walk presses a key.
/// </summary>
public sealed class PageWalkingChaosTests
{
    [Theory]
    [InlineData("pick,click")]
    [InlineData("Type, Press")]
    [InlineData("type,type,type")]
    public void Fault_OfTwoMovesInOneWord_SaysItIsNotAMove(string action)
    {
        var move = new Move(action, Css: "#x", Value: "v");

        Assert.NotNull(PageWalking.Fault(move));
    }

    [Fact]
    public void Fault_OfTwoReactionsInOneWord_SaysItIsNotAReaction()
    {
        var move = new Move("click", Css: "#x", Expect: "request,appears", Pattern: "p");

        Assert.NotNull(PageWalking.Fault(move));
    }
}
