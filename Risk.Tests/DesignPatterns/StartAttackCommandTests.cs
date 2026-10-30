using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.Models;
using Risk.Tests.Helpers;

namespace Risk.Tests.DesignPatterns;

public class StartAttackCommandTests
{
    private static StartAttackCommand Attack(Game game, string? from = null, string? to = null) =>
        new(game.Human().Id, from ?? game.Human().StartingCountryId, to ?? game.EnemyCountry());

    [Fact]
    public void Execute_ValidAttack_StartsTheAttack()
    {
        var game = TestGames.HumanFirst();
        var log = new EventLog(game.Id);

        var result = Attack(game).Execute(game, log);

        Assert.True(result.Success);
        Assert.Equal(game.Human().StartingCountryId, game.ActiveAttack!.AttackerCountryId);
        Assert.Equal(game.EnemyCountry(), game.ActiveAttack.DefenderCountryId);
        Assert.Equal(GamePhase.Attacking, game.Phase);
        Assert.True(game.AttackedThisTurn);

        var logged = Assert.Single(log.Events);
        Assert.Equal(EventType.AttackStarted, logged.Type);
        Assert.Equal(game.Human().Id, logged.PlayerId);
    }

    [Fact]
    public void Execute_NotThePlayersTurn_IsRejected_AndNothingChanges()
    {
        var game = TestGames.HumanFirst();
        game.GiveTurnToAnNpc();
        var log = new EventLog(game.Id);

        var result = Attack(game).Execute(game, log);

        Assert.False(result.Success);
        Assert.Equal("It is not your turn.", result.Error);
        Assert.Null(game.ActiveAttack);
        Assert.Empty(log.Events);
    }

    [Fact]
    public void Execute_SecondAttackInTheSameTurn_IsRejected()
    {
        var game = TestGames.HumanFirst();
        game.AttackedThisTurn = true;

        var result = Attack(game).Execute(game, new EventLog(game.Id));

        Assert.Equal("You have already attacked this turn.", result.Error);
    }

    [Fact]
    public void Execute_FromACountryTheyDoNotOwn_IsRejected()
    {
        var game = TestGames.HumanFirst();

        var result = Attack(game, from: game.EnemyCountry(), to: game.Human().StartingCountryId)
            .Execute(game, new EventLog(game.Id));

        Assert.Equal("You can only attack from a country you own.", result.Error);
    }

    [Fact]
    public void Execute_AttackingYourOwnCountry_IsRejected()
    {
        var game = TestGames.HumanFirst();
        game.Human().OwnedCountryIds.Add(game.EnemyCountry());

        var result = Attack(game).Execute(game, new EventLog(game.Id));

        Assert.Equal("You cannot attack your own country.", result.Error);
    }

    [Fact]
    public void Execute_WithOnlyOneSoldier_IsRejected()
    {
        var game = TestGames.HumanFirst(humanSoldiers: 1);

        var result = Attack(game).Execute(game, new EventLog(game.Id));

        Assert.Equal("You need at least 2 soldiers to attack.", result.Error);
    }

    [Fact]
    public void Execute_UnknownCountry_IsRejected()
    {
        var game = TestGames.HumanFirst();

        var result = Attack(game, to: "ZZ").Execute(game, new EventLog(game.Id));

        Assert.Equal("That country is not in this game.", result.Error);
    }

    [Fact]
    public void Execute_DefenderWithNoSoldiers_IsRejected()
    {
        var game = TestGames.HumanFirst(enemySoldiers: 0);

        var result = Attack(game).Execute(game, new EventLog(game.Id));

        Assert.Equal("That country has no soldiers to fight.", result.Error);
    }

    [Fact]
    public void Execute_WhileAnAttackIsRunning_IsRejected()
    {
        var game = TestGames.HumanFirst();
        Attack(game).Execute(game, new EventLog(game.Id));

        var result = Attack(game).Execute(game, new EventLog(game.Id));

        Assert.Equal("An attack is already in progress.", result.Error);
    }

    [Fact]
    public void Execute_AfterTheGameIsOver_IsRejected()
    {
        var game = TestGames.HumanFirst();
        game.Phase = GamePhase.Finished;

        var result = Attack(game).Execute(game, new EventLog(game.Id));

        Assert.Equal("The game is over.", result.Error);
    }
}