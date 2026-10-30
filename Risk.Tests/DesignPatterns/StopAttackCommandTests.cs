using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.Models;
using Risk.Tests.Helpers;

namespace Risk.Tests.DesignPatterns;

public class StopAttackCommandTests
{
    [Fact]
    public void Execute_EndsTheAttack()
    {
        var game = TestGames.HumanFirst();
        var human = game.Human();
        new StartAttackCommand(human.Id, human.StartingCountryId, game.EnemyCountry())
            .Execute(game, new EventLog(game.Id));
        var log = new EventLog(game.Id);

        var stop = new StopAttackCommand(human.Id).Execute(game, log);

        Assert.True(stop.Success);
        Assert.Null(game.ActiveAttack);
        Assert.Equal(GamePhase.Action, game.Phase);
        Assert.Equal(EventType.AttackEnded, log.Events[0].Type);
    }

    [Fact]
    public void Execute_WithoutAnAttack_IsRejected()
    {
        var game = TestGames.HumanFirst();

        var result = new StopAttackCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        Assert.Equal("There is no attack in progress.", result.Error);
    }

    [Fact]
    public void Execute_Stopping_EndsTheTurnAndPassesItOn()
    {
        var game = TestGames.HumanFirst();
        var human = game.Human();
        new StartAttackCommand(human.Id, human.StartingCountryId, game.EnemyCountry())
            .Execute(game, new EventLog(game.Id));

        var result = new StopAttackCommand(human.Id).Execute(game, new EventLog(game.Id));

        Assert.True(result.Success);
        Assert.NotEqual(human.Id, game.TurnQueue.Peek());
        Assert.False(game.AttackedThisTurn);
    }
}