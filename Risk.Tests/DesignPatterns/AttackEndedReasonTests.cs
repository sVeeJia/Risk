using System.Text.RegularExpressions;
using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.Models;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;

namespace Risk.Tests.DesignPatterns;

public class AttackEndedReasonTests
{
    private static string ReasonOf(EventLog log)
    {
        var reason = log.Events.Single(e => e.Type == EventType.AttackEnded).Data["reason"];

        // A bare lowercase word: no trailing period or other punctuation.
        Assert.Matches(new Regex("^[a-z]+$"), reason);
        return reason;
    }

    private static Game AttackInProgress(int humanSoldiers, int enemySoldiers)
    {
        var game = TestGames.HumanFirst(humanSoldiers, enemySoldiers);
        var human = game.Human();
        new StartAttackCommand(human.Id, human.StartingCountryId, game.EnemyCountry())
            .Execute(game, new EventLog(game.Id));
        return game;
    }

    [Fact]
    public void Stopping_UsesTheBareTokenStopped()
    {
        var game = AttackInProgress(5, 3);
        var log = new EventLog(game.Id);

        new StopAttackCommand(game.Human().Id).Execute(game, log);

        Assert.Equal("stopped", ReasonOf(log));
    }

    [Fact]
    public void RunningOutOfSoldiers_UsesTheBareTokenExhausted()
    {
        var game = AttackInProgress(2, 5);
        var log = new EventLog(game.Id);

        new RollAttackCommand(game.Human().Id, new FakeScriptedRandomDiceRolls(1, 6)).Execute(game, log);

        Assert.Equal("exhausted", ReasonOf(log));
    }

    [Fact]
    public void ConquestFollowedByATransfer_UsesTheBareTokenConquest()
    {
        var game = AttackInProgress(5, 1);
        new RollAttackCommand(game.Human().Id, new FakeScriptedRandomDiceRolls(6, 1))
            .Execute(game, new EventLog(game.Id));
        var log = new EventLog(game.Id);

        new TransferAfterConquestCommand(game.Human().Id, 1).Execute(game, log);

        Assert.Equal("conquest", ReasonOf(log));
    }

    [Fact]
    public void ConquestThatWinsTheGame_UsesTheBareTokenConquest()
    {
        var game = AttackInProgress(5, 1);
        var enemyPlayer = game.Players.First(p => !p.IsHuman);
        foreach (var other in game.Players.Where(p => !p.IsHuman && p.Id != enemyPlayer.Id))
        {
            game.Human().OwnedCountryIds.UnionWith(other.OwnedCountryIds);
            other.OwnedCountryIds.Clear();
            other.IsEliminated = true;
        }
        var log = new EventLog(game.Id);

        new RollAttackCommand(game.Human().Id, new FakeScriptedRandomDiceRolls(6, 1)).Execute(game, log);

        Assert.Equal("conquest", ReasonOf(log));
    }
}