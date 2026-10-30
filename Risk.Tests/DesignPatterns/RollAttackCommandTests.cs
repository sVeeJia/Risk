using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.Engine;
using Risk.Api.Models;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;

namespace Risk.Tests.DesignPatterns;

public class RollAttackCommandTests
{
    // A game where the human has already started an attack on the first NPC's country.
    private static Game AttackInProgress(int humanSoldiers, int enemySoldiers)
    {
        var game = TestGames.HumanFirst(humanSoldiers, enemySoldiers);
        var start = new StartAttackCommand(game.Human().Id, game.Human().StartingCountryId, game.EnemyCountry());
        Assert.True(start.Execute(game, new EventLog(game.Id)).Success);
        return game;
    }

    private static Territory Attacker(Game game) => game.Territories[game.Human().StartingCountryId];
    private static Territory Defender(Game game) => game.Territories[game.EnemyCountry()];

    [Fact]
    public void Execute_WithoutAnAttack_IsRejected()
    {
        var game = TestGames.HumanFirst();

        var result = new RollAttackCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        Assert.Equal("There is no attack in progress.", result.Error);
    }

    [Fact]
    public void Execute_NotThePlayersTurn_IsRejected()
    {
        var game = AttackInProgress(5, 3);
        game.GiveTurnToAnNpc();

        var result = new RollAttackCommand(game.Human().Id).Execute(game, new EventLog(game.Id));

        Assert.Equal("It is not your turn.", result.Error);
    }

    [Fact]
    public void Execute_AppliesTheLossesFromTheDice_AndKeepsTheAttackGoing()
    {
        var game = AttackInProgress(humanSoldiers: 5, enemySoldiers: 3); // 3 dice each
        var log = new EventLog(game.Id);
        // Attacker 6,6,1 against defender 1,1,6: two defender losses, one attacker loss.
        var dice = new FakeScriptedRandomDiceRolls(6, 6, 1, 1, 1, 6);

        var result = new RollAttackCommand(game.Human().Id, dice).Execute(game, log);

        Assert.True(result.Success);
        Assert.Equal(4, Attacker(game).Soldiers);
        Assert.Equal(1, Defender(game).Soldiers);
        Assert.Equal(GamePhase.Attacking, game.Phase);
        Assert.NotNull(game.ActiveAttack);

        var round = Assert.Single(log.Events);
        Assert.Equal(EventType.BattleRound, round.Type);
        Assert.Equal("1", round.Data["attackerLosses"]);
        Assert.Equal("2", round.Data["defenderLosses"]);
    }

    [Fact]
    public void Execute_AttackerDownToOneSoldier_EndsTheAttackWithoutConquering()
    {
        var game = AttackInProgress(humanSoldiers: 2, enemySoldiers: 5); // 1 die each
        var log = new EventLog(game.Id);

        new RollAttackCommand(game.Human().Id, new FakeScriptedRandomDiceRolls(1, 6)).Execute(game, log);

        Assert.Equal(1, Attacker(game).Soldiers);
        Assert.Null(game.ActiveAttack);
        Assert.Equal(GamePhase.Action, game.Phase);
        Assert.True(game.AttackedThisTurn);
        Assert.DoesNotContain(game.Human().OwnedCountryIds, id => id == game.EnemyCountry());
        Assert.Equal(new[] { EventType.BattleRound, EventType.AttackEnded }, log.Events.Select(e => e.Type));
    }
        [Fact]
    public void Execute_DefenderRunsOut_ConquersTheCountry_AndWaitsForTheTransfer()
    {
        var game = AttackInProgress(humanSoldiers: 5, enemySoldiers: 1);
        var enemyPlayer = game.Players.First(p => !p.IsHuman);
        var enemyCountry = game.EnemyCountry();
        var expectedReward = BattleEngine.GetConquestReward(Defender(game).BaseSoldiers);
        var log = new EventLog(game.Id);

        new RollAttackCommand(game.Human().Id, new FakeScriptedRandomDiceRolls(6, 1)).Execute(game, log);

        Assert.Contains(enemyCountry, game.Human().OwnedCountryIds);
        Assert.DoesNotContain(enemyCountry, enemyPlayer.OwnedCountryIds);
        Assert.Equal(5 + expectedReward, Attacker(game).Soldiers); // the reward goes to the attacking country
        Assert.Equal(0, Defender(game).Soldiers);                  // waiting for the player to move soldiers in
        Assert.True(enemyPlayer.IsEliminated);
        Assert.Equal(GamePhase.ConquestTransfer, game.Phase);
        Assert.NotNull(game.ActiveAttack);
        Assert.Equal(
            new[] { EventType.BattleRound, EventType.CountryConquered, EventType.PlayerEliminated },
            log.Events.Select(e => e.Type));
    }

    [Fact]
    public void Execute_Conquest_RewardUsesTheCountrysValue_NotTheSoldiersItHadLeft()
    {
        var game = AttackInProgress(humanSoldiers: 5, enemySoldiers: 1);
        Defender(game).BaseSoldiers = 9; // a value-9 country (reward 4), even though only 1 soldier was left

        new RollAttackCommand(game.Human().Id, new FakeScriptedRandomDiceRolls(6, 1))
            .Execute(game, new EventLog(game.Id));

        Assert.Equal(5 + 4, Attacker(game).Soldiers);
    }

    [Fact]
    public void Execute_ConqueringTheLastCountry_WinsTheGame()
    {
        var game = AttackInProgress(humanSoldiers: 5, enemySoldiers: 1);
        var enemyPlayer = game.Players.First(p => !p.IsHuman);

        // Every other NPC has already lost their country to the human.
        foreach (var other in game.Players.Where(p => !p.IsHuman && p.Id != enemyPlayer.Id))
        {
            game.Human().OwnedCountryIds.UnionWith(other.OwnedCountryIds);
            other.OwnedCountryIds.Clear();
            other.IsEliminated = true;
        }
        var log = new EventLog(game.Id);

        new RollAttackCommand(game.Human().Id, new FakeScriptedRandomDiceRolls(6, 1)).Execute(game, log);

        Assert.Equal(GamePhase.Finished, game.Phase);
        Assert.Equal(game.Human().Id, game.WinnerPlayerId);
        Assert.Null(game.ActiveAttack);
        Assert.Equal(EventType.GameWon, log.Events[^1].Type);
        Assert.Equal(10, game.Human().OwnedCountryIds.Count);
    }

    [Fact]
    public void Execute_UsesTheGamesOwnSeed_SoTheSameGameRollsTheSameWay()
    {
        var first = AttackInProgress(8, 6);
        var second = AttackInProgress(8, 6);
        var startingRolls = first.RngRollsUsed;

        new RollAttackCommand(first.Human().Id).Execute(first, new EventLog(first.Id));
        new RollAttackCommand(second.Human().Id).Execute(second, new EventLog(second.Id));

        Assert.True(first.RngRollsUsed > startingRolls);
        Assert.Equal(first.RngRollsUsed, second.RngRollsUsed);
        Assert.Equal(Attacker(first).Soldiers, Attacker(second).Soldiers);
        Assert.Equal(Defender(first).Soldiers, Defender(second).Soldiers);
    }
}
