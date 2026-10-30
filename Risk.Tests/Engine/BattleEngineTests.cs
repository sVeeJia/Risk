using Risk.Api.Algorithms;
using Risk.Api.Engine;
using Risk.Tests.Fakes;

namespace Risk.Tests.Engine;

public class BattleEngineTests
{
    [Theory]
    [InlineData(5, 3, 3)] // limited by the defender
    [InlineData(2, 9, 1)] // limited by the attacker (one stays behind)
    [InlineData(10, 1, 1)]
    public void ResolveRound_RollsTheCorrectNumberOfDicePerSide(int attacker, int defender, int expectedDice)
    {
        var round = BattleEngine.ResolveRound(attacker, defender, new SeededRandom(1));

        Assert.Equal(expectedDice, round.AttackerDice.Count);
        Assert.Equal(expectedDice, round.DefenderDice.Count);
    }

    [Fact]
    public void ResolveRound_AttackerRollsFirst_ThenDefender()
    {
        var round = BattleEngine.ResolveRound(3, 2, new FakeScriptedRandomDiceRolls(1, 6, 6, 1));

        Assert.Equal(new[] { 1, 6 }, round.AttackerDice);
        Assert.Equal(new[] { 6, 1 }, round.DefenderDice);
    }

    [Fact]
    public void ResolveRound_PairsDiceInOrder_WithoutSorting()
    {
        // Attacker 1,6 against defender 6,1: Die 1 loses, Die 2 wins.
        // Standard sorted Risk would pair 6-6 and 1-1 and give a double tie.
        var round = BattleEngine.ResolveRound(3, 2, new FakeScriptedRandomDiceRolls(1, 6, 6, 1));

        Assert.Equal(1, round.AttackerLosses);
        Assert.Equal(1, round.DefenderLosses);
    }

    [Fact]
    public void ResolveRound_HigherDieWins()
    {
        var round = BattleEngine.ResolveRound(3, 2, new FakeScriptedRandomDiceRolls(6, 6, 1, 2));

        Assert.Equal(0, round.AttackerLosses);
        Assert.Equal(2, round.DefenderLosses);
    }

    [Fact]
    public void ResolveRound_Tie_CostsNobodyAnything()
    {
        var round = BattleEngine.ResolveRound(2, 1, new FakeScriptedRandomDiceRolls(4, 4));

        Assert.Equal(0, round.AttackerLosses);
        Assert.Equal(0, round.DefenderLosses);
    }

    [Fact]
    public void ResolveRound_AttackerWithOneSoldier_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BattleEngine.ResolveRound(1, 3, new SeededRandom(1)));

    [Fact]
    public void ResolveRound_DefenderWithNoSoldiers_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BattleEngine.ResolveRound(5, 0, new SeededRandom(1)));

    [Fact]
    public void ResolveRound_SameSeed_GivesTheSameRound()
    {
        var a = BattleEngine.ResolveRound(6, 4, new SeededRandom(21));
        var b = BattleEngine.ResolveRound(6, 4, new SeededRandom(21));

        Assert.Equal(a.AttackerDice, b.AttackerDice);
        Assert.Equal(a.DefenderDice, b.DefenderDice);
        Assert.Equal(a.AttackerLosses, b.AttackerLosses);
        Assert.Equal(a.DefenderLosses, b.DefenderLosses);
    }

    [Fact]
    public void ResolveRound_OneDieEach_HasTheRightOdds()
    {
        // 36 possible rolls: attacker wins 15, defender wins 15, tie 6.
        int attackerWins = 0, ties = 0;
        for (var seed = 0; seed < 6000; seed++)
        {
            var round = BattleEngine.ResolveRound(2, 1, new SeededRandom(seed));
            if (round.DefenderLosses == 1) attackerWins++;
            else if (round.AttackerLosses == 0) ties++;
        }

        Assert.InRange(attackerWins, 2300, 2700); // about 2500
        Assert.InRange(ties, 850, 1150);          // about 1000
    }

    [Theory]
    [InlineData(5, 0, BattleStatus.Conquered)]
    [InlineData(1, 0, BattleStatus.Conquered)]          // conquering wins over running out
    [InlineData(1, 3, BattleStatus.AttackerExhausted)]
    [InlineData(4, 2, BattleStatus.Ongoing)]
    public void GetStatus_ReportsWhereTheAttackStands(int attacker, int defender, BattleStatus expected) =>
        Assert.Equal(expected, BattleEngine.GetStatus(attacker, defender));

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 2)]
    [InlineData(6, 3)]
    [InlineData(7, 3)]
    [InlineData(8, 4)]
    [InlineData(9, 4)]
    public void GetConquestReward_IsHalfTheBaseSoldiers_RoundedDown_ButAtLeastTwo(int baseSoldiers, int expected) =>
        Assert.Equal(expected, BattleEngine.GetConquestReward(baseSoldiers));
}