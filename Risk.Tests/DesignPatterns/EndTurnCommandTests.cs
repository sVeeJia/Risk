using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.Models;
using Risk.Tests.Helpers;

namespace Risk.Tests.DesignPatterns;

public class EndTurnCommandTests
{
    // Ends the turn of whoever is the current player.
    private static CommandResult EndTurn(Game game, EventLog? log = null) => new EndTurnCommand(game.TurnQueue.Peek()).Execute(game, log ?? new EventLog(game.Id));

    [Fact]
    public void Execute_PassesTheTurnToTheNextPlayer()
    {
        var game = TestGames.HumanFirst();
        var firstNpc = game.Players.First(p => !p.IsHuman);
        var log = new EventLog(game.Id);

        var result = new EndTurnCommand(game.Human().Id).Execute(game, log);

        Assert.True(result.Success);
        Assert.Equal(firstNpc.Id, game.TurnQueue.Peek());
        var started = Assert.Single(log.Events);
        Assert.Equal(EventType.TurnStarted, started.Type);
        Assert.Equal(firstNpc.Id, started.PlayerId);
        Assert.Equal(GamePhase.Action, game.Phase);
    }

    [Fact]
    public void Execute_MovesThePlayerWhoEndedToTheBackOfTheQueue()
    {
        var game = TestGames.HumanFirst();

        new EndTurnCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        Assert.Equal(10, game.TurnQueue.Count);
        Assert.Equal(game.Human().Id, game.TurnQueue.Last());
    }

    [Fact]
    public void Execute_ResetsTheOneAttackPerTurnRule()
    {
        var game = TestGames.HumanFirst();
        game.AttackedThisTurn = true;

        EndTurn(game);

        Assert.False(game.AttackedThisTurn);
    }

    [Fact]
    public void Execute_InRoundOne_GivesNoReinforcements()
    {
        var game = TestGames.HumanFirst();

        EndTurn(game);

        var next = game.Players.Single(p => p.Id == game.TurnQueue.Peek());
        Assert.Equal(1, game.Round);
        Assert.Equal(0, next.PendingReinforcements);
        Assert.Equal(GamePhase.Action, game.Phase);
    }

    [Fact]
    public void Execute_AfterEveryoneHasPlayed_StartsRoundTwoWithReinforcements()
    {
        var game = TestGames.HumanFirst();
        var log = new EventLog(game.Id);

        for (var i = 0; i < 10; i++) Assert.True(EndTurn(game, log).Success);

        Assert.Equal(2, game.Round);
        Assert.Equal(game.Human().Id, game.TurnQueue.Peek());
        Assert.Equal(GamePhase.Reinforcing, game.Phase);
        Assert.Equal(2, game.Human().PendingReinforcements); // 2 per country, the human owns 1
        Assert.Contains(log.Events, e => e.Type == EventType.ReinforcementsReceived && e.PlayerId == game.Human().Id);
    }

    [Fact]
    public void Execute_TheRoundDoesNotAdvanceBeforeEveryonePlayed()
    {
        var game = TestGames.HumanFirst();

        for (var i = 0; i < 9; i++) EndTurn(game);

        Assert.Equal(1, game.Round);
    }

    [Fact]
    public void Execute_ReinforcementsScaleWithTheCountriesOwned()
    {
        var game = TestGames.HumanFirst();
        foreach (var other in game.Players.Where(p => !p.IsHuman).Take(2))
        {
            game.Human().OwnedCountryIds.UnionWith(other.OwnedCountryIds);
            other.OwnedCountryIds.Clear();
            other.IsEliminated = true;
        }

        for (var i = 0; i < 20 && game.Round == 1; i++) EndTurn(game);

        Assert.Equal(2, game.Round);
        Assert.Equal(6, game.Human().PendingReinforcements); // 3 countries x 2
    }

    [Fact]
    public void Execute_SkipsEliminatedPlayers()
    {
        var game = TestGames.HumanFirst();
        var npcs = game.Players.Where(p => !p.IsHuman).ToList();
        npcs[0].IsEliminated = true;
        npcs[0].OwnedCountryIds.Clear();
        var log = new EventLog(game.Id);

        new EndTurnCommand(game.Human().Id).Execute(game, log);

        Assert.Equal(npcs[1].Id, game.TurnQueue.Peek());
        Assert.Equal(new[] { EventType.TurnSkipped, EventType.TurnStarted }, log.Events.Select(e => e.Type));
        Assert.Equal(npcs[0].Id, log.Events[0].PlayerId);
    }

    [Fact]
    public void Execute_AnEliminatedRoundStarter_StillMarksTheStartOfTheNextRound()
    {
        var game = TestGames.HumanFirst();
        var npcs = game.Players.Where(p => !p.IsHuman).ToList();
        game.RoundStartPlayerId = npcs[0].Id;
        npcs[0].IsEliminated = true;
        npcs[0].OwnedCountryIds.Clear();

        new EndTurnCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        Assert.Equal(2, game.Round);
        Assert.Equal(npcs[1].Id, game.TurnQueue.Peek());
        Assert.Equal(GamePhase.Reinforcing, game.Phase);
        Assert.Equal(2, npcs[1].PendingReinforcements);
    }

    [Fact]
    public void Execute_OnAGameSavedWithoutARoundStarter_UsesThePlayerEndingAsTheStarter()
    {
        var game = TestGames.HumanFirst();
        game.RoundStartPlayerId = null;

        new EndTurnCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        Assert.Equal(game.Human().Id, game.RoundStartPlayerId);
    }

    [Fact]
    public void Execute_DuringAnAttack_IsRejected()
    {
        var game = TestGames.HumanFirst();
        var human = game.Human();
        new StartAttackCommand(human.Id, human.StartingCountryId, game.EnemyCountry())
            .Execute(game, new EventLog(game.Id));

        var result = new EndTurnCommand(human.Id).Execute(game, new EventLog(game.Id));

        Assert.Equal("Finish or stop your attack first.", result.Error);
    }

    [Fact]
    public void Execute_WhileWaitingForAConquestTransfer_IsRejected()
    {
        var game = TestGames.HumanFirst();
        game.Phase = GamePhase.ConquestTransfer;

        var result = new EndTurnCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        Assert.Equal("Move soldiers into the conquered country first.", result.Error);
    }

    [Fact]
    public void Execute_WhileReinforcementsAreUnplaced_IsRejected()
    {
        var game = TestGames.HumanFirst();
        game.Phase = GamePhase.Reinforcing;

        var result = new EndTurnCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        Assert.Equal("Place your reinforcements first.", result.Error);
    }

    [Fact]
    public void Execute_NotThePlayersTurn_IsRejected()
    {
        var game = TestGames.HumanFirst();
        game.GiveTurnToAnNpc();

        var result = new EndTurnCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        Assert.Equal("It is not your turn.", result.Error);
    }
}