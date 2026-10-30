using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.Models;
using Risk.Tests.Helpers;

namespace Risk.Tests.DesignPatterns;

public class PlaceReinforcementsCommandTests
{
    // A round-2 game: the human has 4 reinforcements to place.
    private static Game WithReinforcements(int pending = 4)
    {
        var game = TestGames.HumanFirst();
        game.Round = 2;
        game.Phase = GamePhase.Reinforcing;
        game.Human().PendingReinforcements = pending;
        return game;
    }

    private static CommandResult Place(Game game, string countryId, int soldiers, EventLog? log = null) => new PlaceReinforcementsCommand(game.Human().Id, countryId, soldiers)
        .Execute(game, log ?? new EventLog(game.Id));

    [Fact]
    public void Execute_PlacesSoldiers_AndReducesWhatIsPending()
    {
        var game = WithReinforcements(4);
        var home = game.Human().StartingCountryId;
        var log = new EventLog(game.Id);

        var result = Place(game, home, 3, log);

        Assert.True(result.Success);
        Assert.Equal(8, game.Territories[home].Soldiers);
        Assert.Equal(1, game.Human().PendingReinforcements);
        Assert.Equal(GamePhase.Reinforcing, game.Phase);
        var placed = Assert.Single(log.Events);
        Assert.Equal(EventType.ReinforcementsPlaced, placed.Type);
        Assert.Equal("1", placed.Data["remaining"]);
    }

    [Fact]
    public void Execute_PlacingTheLastOnes_MovesToTheActionPhase()
    {
        var game = WithReinforcements(4);

        Place(game, game.Human().StartingCountryId, 4);

        Assert.Equal(0, game.Human().PendingReinforcements);
        Assert.Equal(GamePhase.Action, game.Phase);
    }

    [Fact]
    public void Execute_InACountryTheyDoNotOwn_IsRejected()
    {
        var game = WithReinforcements();

        var result = Place(game, game.EnemyCountry(), 1);

        Assert.Equal("You can only place reinforcements in a country you own.", result.Error);
        Assert.Equal(4, game.Human().PendingReinforcements);
    }

    [Fact]
    public void Execute_ZeroSoldiers_IsRejected()
    {
        var game = WithReinforcements();

        var result = Place(game, game.Human().StartingCountryId, 0);

        Assert.Equal("Place at least 1 soldier.", result.Error);
    }

    [Fact]
    public void Execute_MoreThanTheyHaveLeft_IsRejected()
    {
        var game = WithReinforcements(4);

        var result = Place(game, game.Human().StartingCountryId, 5);

        Assert.Equal("You only have 4 reinforcements left to place.", result.Error);
        Assert.Equal(5, game.Territories[game.Human().StartingCountryId].Soldiers);
    }

    [Fact]
    public void Execute_WhenThereAreNoReinforcements_IsRejected()
    {
        var game = TestGames.HumanFirst(); // action phase

        var result = Place(game, game.Human().StartingCountryId, 1);

        Assert.Equal("You have no reinforcements to place.", result.Error);
    }

    [Fact]
    public void Execute_NotThePlayersTurn_IsRejected()
    {
        var game = WithReinforcements();
        game.GiveTurnToAnNpc();

        var result = Place(game, game.Human().StartingCountryId, 1);

        Assert.Equal("It is not your turn.", result.Error);
    }
}