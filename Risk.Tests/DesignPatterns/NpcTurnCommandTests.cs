using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.DesignPatterns.Strategy;
using Risk.Api.Models;
using Risk.Tests.Helpers;

namespace Risk.Tests.DesignPatterns;

public class NpcTurnCommandTests
{
    private readonly NpcTurnCommand _command = new(new OpportunistStrategy());

    // Every NPC country gets otherSoldiers, except the first NPC (npcSoldiers) and the human (humanSoldiers).
    // The first NPC then holds the turn.
    private static (Game Game, Player Npc) NpcTurn(int npcSoldiers, int humanSoldiers, int otherSoldiers)
    {
        var game = TestGames.HumanFirst();
        var npc = game.Players.First(p => !p.IsHuman);

        foreach (var territory in game.Territories.Values)
        {
            var owner = game.Players.First(p => p.OwnedCountryIds.Contains(territory.CountryId));
            var soldiers = owner.Id == npc.Id ? npcSoldiers : owner.IsHuman ? humanSoldiers : otherSoldiers;
            territory.BaseSoldiers = soldiers;
            territory.Soldiers = soldiers;
        }

        var order = new[] { npc.Id }.Concat(game.Players.Select(p => p.Id).Where(id => id != npc.Id)).ToList();
        game.TurnQueue.Clear();
        foreach (var id in order) game.TurnQueue.Enqueue(id);
        game.RoundStartPlayerId = npc.Id;

        return (game, npc);
    }

    [Fact]
    public void Execute_OnTheHumansTurn_IsRejected()
    {
        var game = TestGames.HumanFirst();

        var result = _command.Execute(game, new EventLog(game.Id));

        Assert.Equal("It is your turn.", result.Error);
    }

    [Fact]
    public void Execute_WhenTheGameIsOver_IsRejected()
    {
        var (game, _) = NpcTurn(5, 5, 5);
        game.Phase = GamePhase.Finished;

        var result = _command.Execute(game, new EventLog(game.Id));

        Assert.Equal("The game is over.", result.Error);
    }

    [Fact]
    public void Execute_PlaysTheTurn_AndPassesItOn_WithJustifiedDecisions()
    {
        var (game, npc) = NpcTurn(5, 5, 5);
        var log = new EventLog(game.Id);

        var result = _command.Execute(game, log);

        Assert.True(result.Success);
        Assert.NotEqual(npc.Id, game.TurnQueue.Peek());
        var decisions = log.Events.Where(e => e.Type == EventType.NpcDecision).ToList();
        Assert.NotEmpty(decisions);
        Assert.All(decisions, d => Assert.False(string.IsNullOrWhiteSpace(d.Justification)));
    }

    [Fact]
    public void Execute_AStrongNpcAttacksAWeakNeighbour()
    {
        var (game, _) = NpcTurn(npcSoldiers: 9, humanSoldiers: 1, otherSoldiers: 9);
        var log = new EventLog(game.Id);

        _command.Execute(game, log);

        var started = log.Events.First(e => e.Type == EventType.AttackStarted);
        var decisionIndex = log.Events.ToList().FindIndex(e => e.Type == EventType.NpcDecision);
        Assert.True(decisionIndex < log.Events.ToList().IndexOf(started)); // the reason comes before the action
        Assert.Contains(log.Events, e => e.Type == EventType.BattleRound);
    }

    [Fact]
    public void Execute_WithNothingWorthAttacking_SkipsAndEndsTheTurn()
    {
        var (game, npc) = NpcTurn(npcSoldiers: 2, humanSoldiers: 9, otherSoldiers: 9);
        var log = new EventLog(game.Id);

        _command.Execute(game, log);

        Assert.DoesNotContain(log.Events, e => e.Type == EventType.AttackStarted);
        Assert.Contains(log.Events, e => e.Type == EventType.NpcDecision && e.Message.Contains("does not attack"));
        Assert.NotEqual(npc.Id, game.TurnQueue.Peek());
    }

    [Fact]
    public void Execute_InRoundTwo_PlacesReinforcementsFirst()
    {
        var (game, npc) = NpcTurn(5, 5, 5);
        game.Round = 2;
        game.Phase = GamePhase.Reinforcing;
        npc.PendingReinforcements = 2;
        var log = new EventLog(game.Id);

        var result = _command.Execute(game, log);

        Assert.True(result.Success);
        Assert.Equal(0, npc.PendingReinforcements);
        Assert.Contains(log.Events, e => e.Type == EventType.ReinforcementsPlaced && e.PlayerId == npc.Id);
    }

    [Fact]
    public void Execute_WhenTheHumanHasNoCountriesLeft_EndsTheGame()
    {
        var (game, _) = NpcTurn(5, 5, 5);
        var human = game.Human();
        human.OwnedCountryIds.Clear();
        human.IsEliminated = true;
        var log = new EventLog(game.Id);

        _command.Execute(game, log);

        Assert.Equal(GamePhase.Finished, game.Phase);
        Assert.Null(game.WinnerPlayerId);
        Assert.Contains(log.Events, e => e.Type == EventType.GameLost);
    }

    [Fact]
    public void Execute_RepeatedTurns_EventuallyReturnTheTurnToTheHuman()
    {
        var game = TestGames.HumanFirst();
        new EndTurnCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        for (var turns = 0; turns < 20 && game.Phase != GamePhase.Finished && game.TurnQueue.Peek() != game.Human().Id; turns++)
        {
            var result = _command.Execute(game, new EventLog(game.Id));
            Assert.True(result.Success, result.Error);
        }

        Assert.True(game.Phase == GamePhase.Finished || game.TurnQueue.Peek() == game.Human().Id);
    }
}
