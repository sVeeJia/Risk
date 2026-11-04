using Risk.Api.Commentary;
using Risk.Api.DataStructures;
using Risk.Api.Models;
using Risk.Tests.Helpers;

namespace Risk.Tests.Commentary;

public class RoundFactsBuilderTests
{
    private readonly Game _game = TestGames.HumanFirst();

    private Player Human => _game.Human();
    private string Mine => Human.StartingCountryId;
    private string Enemy => _game.EnemyCountry();
    private string NameOf(string countryId) => _game.Territories[countryId].Name;
    private EventLog NewLog() => new(_game.Id);

    [Fact]
    public void Build_DescribesARoll()
    {
        var log = NewLog();
        log.Round(Human.Id, Mine, Enemy, attackerLosses: 1, defenderLosses: 2, attackerSoldiers: 4, defenderSoldiers: 1, attackerDice: "6,2", defenderDice: "4,3");

        var round = Assert.Single(RoundFactsBuilder.Build(_game, log.Events));

        Assert.Equal(log.Events[0].Sequence, round.Sequence);
        Assert.Equal(Human.Id, round.AttackerPlayerId);
        Assert.True(round.AttackerIsHuman);
        Assert.Equal(Mine, round.AttackerCountryId);
        Assert.Equal(NameOf(Mine), round.AttackerCountry);
        Assert.Equal(Enemy, round.DefenderCountryId);
        Assert.Equal(NameOf(Enemy), round.DefenderCountry);
        Assert.Equal("6,2", round.AttackerDice);
        Assert.Equal("4,3", round.DefenderDice);
        Assert.Equal(1, round.AttackerLosses);
        Assert.Equal(2, round.DefenderLosses);
        Assert.Equal(4, round.AttackerSoldiers);
        Assert.Equal(1, round.DefenderSoldiers);
    }

    [Fact]
    public void Build_ALoneRoll_Continues()
    {
        var log = NewLog();
        log.Round(Human.Id, Mine, Enemy);

        Assert.Equal(RoundResult.Continues, Assert.Single(RoundFactsBuilder.Build(_game, log.Events)).Result);
    }

    [Fact]
    public void Build_ARollFollowedByAConquest_IsMarkedConquered()
    {
        var log = NewLog();
        log.Round(Human.Id, Mine, Enemy);
        log.Conquered(Human.Id, Enemy);

        Assert.Equal(RoundResult.Conquered, Assert.Single(RoundFactsBuilder.Build(_game, log.Events)).Result);
    }

    [Fact]
    public void Build_ARollFollowedByTheAttackerRunningOut_IsMarkedExhausted()
    {
        var log = NewLog();
        log.Round(Human.Id, Mine, Enemy);
        log.Ended(Human.Id, "exhausted");

        Assert.Equal(RoundResult.AttackerExhausted, Assert.Single(RoundFactsBuilder.Build(_game, log.Events)).Result);
    }

    [Fact]
    public void Build_GivesOneEntryPerRoll_InOrder()
    {
        var log = NewLog();
        log.Round(Human.Id, Mine, Enemy);
        log.Round(Human.Id, Mine, Enemy);
        log.Round(Human.Id, Mine, Enemy);

        var rounds = RoundFactsBuilder.Build(_game, log.Events);

        Assert.Equal(new[] { 1, 2, 3 }, rounds.Select(r => r.Sequence));
    }

    [Fact]
    public void Build_SkipsARollThatNamesNoCountries()
    {
        var log = NewLog();
        log.Append(EventType.BattleRound, Human.Id, "A roll saved before countries were recorded.");

        Assert.Empty(RoundFactsBuilder.Build(_game, log.Events));
    }

    [Fact]
    public void Build_CleansCountryNamesToOneLineOfPlainText()
    {
        _game.Territories[Enemy].Name = "Jor\ndan <b>";
        var log = NewLog();
        log.Round(Human.Id, Mine, Enemy);

        var round = Assert.Single(RoundFactsBuilder.Build(_game, log.Events));

        Assert.DoesNotContain("\n", round.DefenderCountry);
        Assert.DoesNotContain("<", round.DefenderCountry);
    }
}