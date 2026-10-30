using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.Engine;
using Risk.Api.Models;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;

namespace Risk.Tests.DesignPatterns;

public class TransferAfterConquestCommandTests
{
    // A game where the human has just conquered the first NPC's country and must now move soldiers in.
    private static Game JustConquered()
    {
        var game = TestGames.HumanFirst(humanSoldiers: 5, enemySoldiers: 1);
        var human = game.Human();
        new StartAttackCommand(human.Id, human.StartingCountryId, game.EnemyCountry())
            .Execute(game, new EventLog(game.Id));
        new RollAttackCommand(human.Id, new FakeScriptedRandomDiceRolls(6, 1))
            .Execute(game, new EventLog(game.Id));

        Assert.Equal(GamePhase.ConquestTransfer, game.Phase);
        return game;
    }

    private static Territory Attacker(Game game) => game.Territories[game.Human().StartingCountryId];
    private static Territory Conquered(Game game) => game.Territories[game.EnemyCountry()];

    private static CommandResult Move(Game game, int soldiers, EventLog? log = null) =>
        new TransferAfterConquestCommand(game.Human().Id, soldiers).Execute(game, log ?? new EventLog(game.Id));

    [Fact]
    public void Execute_MovesTheChosenSoldiers_AndEndsTheAttack()
    {
        var game = JustConquered();
        var attackerBefore = Attacker(game).Soldiers;
        var log = new EventLog(game.Id);

        var result = Move(game, 2, log);

        Assert.True(result.Success);
        Assert.Equal(attackerBefore - 2, Attacker(game).Soldiers);
        Assert.Equal(2, Conquered(game).Soldiers);
        Assert.Equal(GamePhase.Action, game.Phase);
        Assert.Null(game.ActiveAttack);
        Assert.True(game.AttackedThisTurn);
        Assert.Equal(new[] { EventType.Transfer, EventType.AttackEnded }, log.Events.Select(e => e.Type));
    }

    [Fact]
    public void Execute_MovingOneSoldier_IsAllowed()
    {
        var game = JustConquered();

        Assert.True(Move(game, 1).Success);
        Assert.Equal(1, Conquered(game).Soldiers);
    }

    [Fact]
    public void Execute_MovingTheMaximum_LeavesExactlyOneSoldierBehind()
    {
        var game = JustConquered();
        var movable = Attacker(game).Soldiers - 1;

        Assert.True(Move(game, movable).Success);
        Assert.Equal(1, Attacker(game).Soldiers);
        Assert.Equal(movable, Conquered(game).Soldiers);
    }

    [Fact]
    public void Execute_ZeroSoldiers_IsRejected_AndTheGameWaitsForAValidMove()
    {
        var game = JustConquered();

        var result = Move(game, 0);

        Assert.Equal("Move at least 1 soldier into the conquered country.", result.Error);
        Assert.Equal(GamePhase.ConquestTransfer, game.Phase);
        Assert.Equal(0, Conquered(game).Soldiers);
    }

    [Fact]
    public void Execute_MovingEverySoldier_IsRejected_BecauseOneMustStay()
    {
        var game = JustConquered();
        var all = Attacker(game).Soldiers;

        var result = Move(game, all);

        Assert.False(result.Success);
        Assert.Equal(all, Attacker(game).Soldiers);
        Assert.Equal(GamePhase.ConquestTransfer, game.Phase);
    }

    [Fact]
    public void Execute_WhenNothingWasConquered_IsRejected()
    {
        var game = TestGames.HumanFirst();

        var result = Move(game, 1);

        Assert.Equal("There is no conquered country waiting for soldiers.", result.Error);
    }

    [Fact]
    public void Execute_NotThePlayersTurn_IsRejected()
    {
        var game = JustConquered();
        game.GiveTurnToAnNpc();

        var result = Move(game, 1);

        Assert.Equal("It is not your turn.", result.Error);
    }
}