using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.DesignPatterns.Strategy;
using Risk.Api.Models;
using Risk.Tests.Helpers;

namespace Risk.Tests.DesignPatterns;

public class OpportunistStrategyTests
{
    private readonly OpportunistStrategy _strategy = new();

    // Every NPC country gets otherSoldiers, except the first NPC (the one under test) and the human.
    private static (Game Game, Player Npc) Setup(int npcSoldiers, int humanSoldiers, int otherSoldiers)
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

        return (game, npc);
    }

    // The NPC takes a second country from another NPC, so it has two to choose between.
    private static string GiveSecondCountry(Game game, Player npc)
    {
        var other = game.Players.First(p => !p.IsHuman && p.Id != npc.Id);
        npc.OwnedCountryIds.UnionWith(other.OwnedCountryIds);
        other.OwnedCountryIds.Clear();
        other.IsEliminated = true;
        return other.StartingCountryId;
    }

    private static void StartAttack(Game game, Player npc, string targetCountryId)
    {
        var order = new[] { npc.Id }.Concat(game.Players.Select(p => p.Id).Where(id => id != npc.Id)).ToList();
        game.TurnQueue.Clear();
        foreach (var id in order) game.TurnQueue.Enqueue(id);

        var result = new StartAttackCommand(npc.Id, npc.StartingCountryId, targetCountryId)
            .Execute(game, new EventLog(game.Id));
        Assert.True(result.Success);
    }

    [Fact]
    public void ChooseAttack_PicksTheTargetWithTheBestOdds()
    {
        var (game, npc) = Setup(npcSoldiers: 6, humanSoldiers: 1, otherSoldiers: 9);

        var choice = _strategy.ChooseAttack(game, npc);

        Assert.NotNull(choice);
        Assert.Equal(npc.StartingCountryId, choice!.FromCountryId);
        Assert.Equal(game.Human().StartingCountryId, choice.ToCountryId);
        Assert.True(choice.WinChance > 0.9);
    }

    [Fact]
    public void ChooseAttack_ExplainsItself()
    {
        var (game, npc) = Setup(6, 1, 9);

        var choice = _strategy.ChooseAttack(game, npc);

        Assert.Contains(game.Territories[game.Human().StartingCountryId].Name, choice!.Justification);
        Assert.Contains("%", choice.Justification);
    }

    [Fact]
    public void ChooseAttack_WhenEveryoneIsTooStrong_Skips()
    {
        var (game, npc) = Setup(npcSoldiers: 2, humanSoldiers: 9, otherSoldiers: 9);

        Assert.Null(_strategy.ChooseAttack(game, npc));
    }

    [Fact]
    public void ChooseAttack_WithOnlyOneSoldier_Skips()
    {
        var (game, npc) = Setup(npcSoldiers: 1, humanSoldiers: 1, otherSoldiers: 1);

        Assert.Null(_strategy.ChooseAttack(game, npc));
    }

    [Fact]
    public void DecideContinue_WhileTheOddsAreGood_KeepsAttacking()
    {
        var (game, npc) = Setup(6, 1, 9);
        StartAttack(game, npc, game.Human().StartingCountryId);

        var decision = _strategy.DecideContinue(game, npc);

        Assert.True(decision.Continue);
        Assert.NotEmpty(decision.Justification);
    }

    [Fact]
    public void DecideContinue_WhenTheOddsTurnBad_Stops()
    {
        var (game, npc) = Setup(2, 9, 9);
        StartAttack(game, npc, game.Human().StartingCountryId);

        var decision = _strategy.DecideContinue(game, npc);

        Assert.False(decision.Continue);
        Assert.StartsWith("Stops the attack", decision.Justification);
    }

    [Fact]
    public void PlanReinforcements_PutsEverythingInTheCountryThatAttacksBest()
    {
        var (game, npc) = Setup(npcSoldiers: 5, humanSoldiers: 3, otherSoldiers: 9);
        var second = GiveSecondCountry(game, npc);
        game.Territories[second].Soldiers = 1;
        npc.PendingReinforcements = 4;

        var plan = _strategy.PlanReinforcements(game, npc);

        var placement = Assert.Single(plan);
        Assert.Equal(npc.StartingCountryId, placement.CountryId);
        Assert.Equal(4, placement.Soldiers);
        Assert.NotEmpty(placement.Justification);
    }

    [Fact]
    public void PlanReinforcements_WithNothingPending_PlacesNothing()
    {
        var (game, npc) = Setup(5, 3, 9);

        Assert.Empty(_strategy.PlanReinforcements(game, npc));
    }

    [Fact]
    public void ChooseConquestTransfer_MovesHalfOfTheSoldiers()
    {
        var (game, npc) = Setup(7, 1, 9);
        StartAttack(game, npc, game.Human().StartingCountryId);

        var choice = _strategy.ChooseConquestTransfer(game, npc);

        Assert.Equal(3, choice.Soldiers);
        Assert.Equal(npc.StartingCountryId, choice.FromCountryId);
        Assert.Equal(game.Human().StartingCountryId, choice.ToCountryId);
    }

    [Fact]
    public void ChooseConquestTransfer_WithTwoSoldiers_MovesTheOnlyOneItCan()
    {
        var (game, npc) = Setup(2, 1, 9);
        StartAttack(game, npc, game.Human().StartingCountryId);

        Assert.Equal(1, _strategy.ChooseConquestTransfer(game, npc).Soldiers);
    }

    [Fact]
    public void ChooseTransfer_EvensOutAnUnbalancedEmpire()
    {
        var (game, npc) = Setup(npcSoldiers: 9, humanSoldiers: 3, otherSoldiers: 9);
        var second = GiveSecondCountry(game, npc);
        game.Territories[second].Soldiers = 1;

        var choice = _strategy.ChooseTransfer(game, npc);

        Assert.NotNull(choice);
        Assert.Equal(npc.StartingCountryId, choice!.FromCountryId);
        Assert.Equal(second, choice.ToCountryId);
        Assert.Equal(4, choice.Soldiers);
    }

    [Fact]
    public void ChooseTransfer_WhenAlreadyBalanced_DoesNothing()
    {
        var (game, npc) = Setup(npcSoldiers: 3, humanSoldiers: 3, otherSoldiers: 9);
        var second = GiveSecondCountry(game, npc);
        game.Territories[second].Soldiers = 2;

        Assert.Null(_strategy.ChooseTransfer(game, npc));
    }

    [Fact]
    public void ChooseTransfer_WithOneCountry_DoesNothing()
    {
        var (game, npc) = Setup(9, 3, 9);

        Assert.Null(_strategy.ChooseTransfer(game, npc));
    }
}