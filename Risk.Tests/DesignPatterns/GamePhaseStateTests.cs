using Risk.Api.DesignPatterns.State;
using Risk.Api.Models;

namespace Risk.Tests.DesignPatterns;

public class GamePhaseStateTests
{
    [Fact]
    public void ActionState_AllowsStartingAnAttack_ButNothingElse()
    {
        var state = new ActionState();

        Assert.Null(state.CheckCanStartAttack());
        Assert.NotNull(state.CheckCanRoll());
        Assert.NotNull(state.CheckCanStopAttack());
        Assert.NotNull(state.CheckCanTransferAfterConquest());
    }

    [Fact]
    public void AttackingState_AllowsRollingAndStopping_ButNotStartingAnotherOrTransferring()
    {
        var state = new AttackingState();

        Assert.NotNull(state.CheckCanStartAttack());
        Assert.Null(state.CheckCanRoll());
        Assert.Null(state.CheckCanStopAttack());
        Assert.NotNull(state.CheckCanTransferAfterConquest());
    }

    [Fact]
    public void ConquestTransferState_AllowsOnlyTheTransfer()
    {
        var state = new ConquestTransferState();

        Assert.Equal("Move soldiers into the conquered country first.", state.CheckCanStartAttack());
        Assert.NotNull(state.CheckCanRoll());
        Assert.NotNull(state.CheckCanStopAttack());
        Assert.Null(state.CheckCanTransferAfterConquest());
    }

    [Fact]
    public void ReinforcingState_BlocksEverythingUntilReinforcementsArePlaced()
    {
        var state = new ReinforcingState();

        Assert.Equal("Place your reinforcements first.", state.CheckCanStartAttack());
        Assert.NotNull(state.CheckCanRoll());
        Assert.NotNull(state.CheckCanStopAttack());
        Assert.NotNull(state.CheckCanTransferAfterConquest());
    }

    [Fact]
    public void FinishedState_BlocksEverything()
    {
        var state = new FinishedState();

        Assert.Equal("The game is over.", state.CheckCanStartAttack());
        Assert.Equal("The game is over.", state.CheckCanRoll());
        Assert.Equal("The game is over.", state.CheckCanStopAttack());
        Assert.Equal("The game is over.", state.CheckCanTransferAfterConquest());
    }

    [Theory]
    [InlineData(GamePhase.Reinforcing, typeof(ReinforcingState))]
    [InlineData(GamePhase.Action, typeof(ActionState))]
    [InlineData(GamePhase.Attacking, typeof(AttackingState))]
    [InlineData(GamePhase.ConquestTransfer, typeof(ConquestTransferState))]
    [InlineData(GamePhase.Finished, typeof(FinishedState))]
    public void For_ReturnsTheStateOfEachPhase(GamePhase phase, Type expected) =>
        Assert.IsType(expected, GamePhaseStates.For(phase));
}