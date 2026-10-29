using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.Models;
using Risk.Tests.Helpers;

namespace Risk.Tests.DesignPatterns;

public class StopAttackCommandTests
{
    [Fact]
    public void Execute_EndsTheAttack_ButTheOneAttackPerTurnRuleStillApplies()
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
        Assert.Equal(EventType.AttackEnded, Assert.Single(log.Events).Type);

        var again = new StartAttackCommand(human.Id, human.StartingCountryId, game.EnemyCountry())
            .Execute(game, new EventLog(game.Id));
        Assert.Equal("You have already attacked this turn.", again.Error);
    }

    [Fact]
    public void Execute_WithoutAnAttack_IsRejected()
    {
        var game = TestGames.HumanFirst();

        var result = new StopAttackCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        Assert.Equal("There is no attack in progress.", result.Error);
    }
}