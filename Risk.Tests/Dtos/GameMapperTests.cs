using Risk.Api.DesignPatterns.Factory;
using Risk.Api.Dtos.Mappers;
using Risk.Api.Models;

namespace Risk.Tests.Dtos;

public class GameMapperTests
{
    private static Game NewGame()
    {
        var playableCountries = Enumerable.Range(1, 42)
            .Select(i => new Country { Id = $"C{i:00}", Name = $"Country {i}", Population = i * 5_000_000L, Rank = i })
            .ToList();
        return new GameFactory().Create("user-1", "Victor", "C07", playableCountries, 1, DateTime.UtcNow);
    }

    [Fact]
    public void ToResponse_HumanCountryIsSlotOne_AndSlotsRunOneToTen()
    {
        var response = GameMapper.ToResponse(NewGame());

        Assert.Equal("C07", response.Territories[0].CountryId);
        Assert.Equal(Enumerable.Range(1, 10), response.Territories.Select(t => t.Slot));
    }

    [Fact]
    public void ToResponse_CurrentPlayerIsTheFrontOfTheTurnQueue()
    {
        var game = NewGame();
        var response = GameMapper.ToResponse(game);

        Assert.Equal(game.TurnQueue.Peek(), response.CurrentPlayerId);
        Assert.Equal("Action", response.Phase);
    }

    [Fact]
    public void ToResponse_EachTerritoryIsOwnedByItsStartingPlayer()
    {
        var game = NewGame();
        var response = GameMapper.ToResponse(game);

        foreach (var territory in response.Territories)
        {
            var owner = game.Players.Single(p => p.Id == territory.OwnerPlayerId);
            Assert.Equal(territory.CountryId, owner.StartingCountryId);
        }
    }
}