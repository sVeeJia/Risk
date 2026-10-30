using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.Models;
using Risk.Tests.Helpers;

namespace Risk.Tests.DesignPatterns;

public class TransferCommandTests
{
    // The human owns two countries: home (5 soldiers) and a second one (3 soldiers).
    private static Game WithTwoCountries()
    {
        var game = TestGames.HumanFirst(humanSoldiers: 5, enemySoldiers: 3);
        var npc = game.Players.First(p => !p.IsHuman);
        game.Human().OwnedCountryIds.UnionWith(npc.OwnedCountryIds);
        npc.OwnedCountryIds.Clear();
        return game;
    }

    private static string Home(Game game) => game.Human().StartingCountryId;
    private static string Second(Game game) => game.EnemyCountry();

    private static CommandResult Move(Game game, string from, string to, int soldiers, EventLog? log = null) =>
        new TransferCommand(game.Human().Id, from, to, soldiers).Execute(game, log ?? new EventLog(game.Id));

    [Fact]
    public void Execute_MovesSoldiersBetweenTheirCountries()
    {
        var game = WithTwoCountries();
        var log = new EventLog(game.Id);

        var result = Move(game, Home(game), Second(game), 3, log);

        Assert.True(result.Success);
        Assert.Equal(2, game.Territories[Home(game)].Soldiers);
        Assert.Equal(6, game.Territories[Second(game)].Soldiers);
        Assert.Equal(GamePhase.Action, game.Phase);
        Assert.Equal(EventType.Transfer, Assert.Single(log.Events).Type);
    }

    [Fact]
    public void Execute_MovingTheMaximum_LeavesExactlyOneSoldierBehind()
    {
        var game = WithTwoCountries();

        Assert.True(Move(game, Home(game), Second(game), 4).Success);
        Assert.Equal(1, game.Territories[Home(game)].Soldiers);
    }

    [Fact]
    public void Execute_MovingEverySoldier_IsRejected()
    {
        var game = WithTwoCountries();

        var result = Move(game, Home(game), Second(game), 5);

        Assert.False(result.Success);
        Assert.Equal(5, game.Territories[Home(game)].Soldiers);
    }

    [Fact]
    public void Execute_ZeroSoldiers_IsRejected()
    {
        var game = WithTwoCountries();

        Assert.Equal("Move at least 1 soldier.", Move(game, Home(game), Second(game), 0).Error);
    }

    [Fact]
    public void Execute_SameCountryTwice_IsRejected()
    {
        var game = WithTwoCountries();

        Assert.Equal("Choose two different countries.", Move(game, Home(game), Home(game), 1).Error);
    }

    [Fact]
    public void Execute_IntoACountryTheyDoNotOwn_IsRejected()
    {
        var game = TestGames.HumanFirst();

        var result = Move(game, Home(game), game.EnemyCountry(), 1);

        Assert.Equal("You can only move soldiers between countries you own.", result.Error);
    }

    [Fact]
    public void Execute_FromACountryTheyDoNotOwn_IsRejected()
    {
        var game = TestGames.HumanFirst();

        var result = Move(game, game.EnemyCountry(), Home(game), 1);

        Assert.Equal("You can only move soldiers between countries you own.", result.Error);
    }

    [Fact]
    public void Execute_WhileAnAttackIsRunning_IsRejected()
    {
        var game = WithTwoCountries();
        var other = game.Players.Where(p => !p.IsHuman && p.OwnedCountryIds.Count > 0).First().StartingCountryId;
        new StartAttackCommand(game.Human().Id, Home(game), other).Execute(game, new EventLog(game.Id));

        var result = Move(game, Home(game), Second(game), 1);

        Assert.Equal("You cannot move soldiers while an attack is in progress.", result.Error);
    }

    [Fact]
    public void Execute_AfterTheAttackHasEnded_IsAllowed()
    {
        var game = WithTwoCountries();
        var other = game.Players.Where(p => !p.IsHuman && p.OwnedCountryIds.Count > 0).First().StartingCountryId;
        new StartAttackCommand(game.Human().Id, Home(game), other).Execute(game, new EventLog(game.Id));
        new StopAttackCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        Assert.True(Move(game, Home(game), Second(game), 1).Success);
    }

    [Fact]
    public void Execute_NotThePlayersTurn_IsRejected()
    {
        var game = WithTwoCountries();
        game.GiveTurnToAnNpc();

        Assert.Equal("It is not your turn.", Move(game, Home(game), Second(game), 1).Error);
    }
}