using Risk.Api.DesignPatterns.State;
using Risk.Api.Models;

namespace Risk.Tests.DesignPatterns;

public class GamePhaseStateTests
{
    [Fact]
    public void ActionState_AllowsAttackingTransferringAndEndingTheTurn()
    {
        var state = new ActionState();

        Assert.Null(state.CheckCanStartAttack());
        Assert.Null(state.CheckCanTransfer());
        Assert.Null(state.CheckCanEndTurn());
        Assert.NotNull(state.CheckCanRoll());
        Assert.NotNull(state.CheckCanStopAttack());
        Assert.NotNull(state.CheckCanTransferAfterConquest());
        Assert.NotNull(state.CheckCanPlaceReinforcements());
    }

    [Fact]
    public void AttackingState_AllowsOnlyRollingAndStopping()
    {
        var state = new AttackingState();

        Assert.Null(state.CheckCanRoll());
        Assert.Null(state.CheckCanStopAttack());
        Assert.NotNull(state.CheckCanStartAttack());
        Assert.NotNull(state.CheckCanTransfer());
        Assert.Equal("Finish or stop your attack first.", state.CheckCanEndTurn());
        Assert.NotNull(state.CheckCanTransferAfterConquest());
        Assert.NotNull(state.CheckCanPlaceReinforcements());
    }

    [Fact]
    public void ConquestTransferState_AllowsOnlyTheTransfer()
    {
        var state = new ConquestTransferState();

        Assert.Null(state.CheckCanTransferAfterConquest());
        Assert.Equal("Move soldiers into the conquered country first.", state.CheckCanStartAttack());
        Assert.NotNull(state.CheckCanRoll());
        Assert.NotNull(state.CheckCanStopAttack());
        Assert.NotNull(state.CheckCanTransfer());
        Assert.NotNull(state.CheckCanEndTurn());
        Assert.NotNull(state.CheckCanPlaceReinforcements());
    }

    [Fact]
    public void ReinforcingState_AllowsOnlyPlacingReinforcements()
    {
        var state = new ReinforcingState();

        Assert.Null(state.CheckCanPlaceReinforcements());
        Assert.Equal("Place your reinforcements first.", state.CheckCanStartAttack());
        Assert.Equal("Place your reinforcements first.", state.CheckCanTransfer());
        Assert.Equal("Place your reinforcements first.", state.CheckCanEndTurn());
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
        Assert.Equal("The game is over.", state.CheckCanPlaceReinforcements());
        Assert.Equal("The game is over.", state.CheckCanTransfer());
        Assert.Equal("The game is over.", state.CheckCanEndTurn());
    }

    [Theory]
    [InlineData(GamePhase.Reinforcing, typeof(ReinforcingState))]
    [InlineData(GamePhase.Action, typeof(ActionState))]
    [InlineData(GamePhase.Attacking, typeof(AttackingState))]
    [InlineData(GamePhase.ConquestTransfer, typeof(ConquestTransferState))]
    [InlineData(GamePhase.Finished, typeof(FinishedState))]
    public void For_ReturnsTheStateOfEachPhase(GamePhase phase, Type expected) => Assert.IsType(expected, GamePhaseStates.For(phase));
}