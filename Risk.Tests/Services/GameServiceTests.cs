using Microsoft.Extensions.Configuration;
using Risk.Api.DesignPatterns.Factory;
using Risk.Api.Models;
using Risk.Api.Services;
using Risk.Api.Services.Interfaces;
using Risk.Tests.Fakes;

namespace Risk.Tests.Services;

public class GameServiceTests
{
    private static (GameService Service, FakeGameRepository Games) Create(
        int countryCount = 60, int playableCountries = 42)
    {
        var countries = new FakeCountryRepository();
        countries.Items.AddRange(Enumerable.Range(1, countryCount).Select(i => new Country
        {
            Id = $"C{i:00}",
            Name = $"Country {i}",
            Population = i * 5_000_000L,
            Rank = i
        }));

        var games = new FakeGameRepository();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Game:PlayableCountries"] = playableCountries.ToString()
            })
            .Build();

        return (new GameService(countries, games, new GameFactory(), config), games);
    }

    [Fact]
    public async Task StartGame_ValidCountry_SavesAGameForTheUser()
    {
        var (service, games) = Create();

        var result = await service.StartGameAsync("user-1", "Victor", "C07");

        Assert.Equal(StartGameStatus.Success, result.Status);
        var saved = Assert.Single(games.Items);
        Assert.Equal("user-1", saved.OwnerUserId);
        Assert.Equal("C07", saved.Players.Single(p => p.IsHuman).StartingCountryId);
    }

    [Fact]
    public async Task StartGame_IsCaseInsensitive()
    {
        var (service, _) = Create();
        var result = await service.StartGameAsync("user-1", "Victor", "c07");
        Assert.Equal(StartGameStatus.Success, result.Status);
    }

    [Fact]
    public async Task StartGame_CountryNotPlayable_IsRejected_AndNothingIsSaved()
    {
        var (service, games) = Create(countryCount: 60, playableCountries: 42);

        var result = await service.StartGameAsync("user-1", "Victor", "C50");

        Assert.Equal(StartGameStatus.InvalidCountry, result.Status);
        Assert.Empty(games.Items);
    }

    [Fact]
    public async Task StartGame_UnknownCountry_IsRejected()
    {
        var (service, games) = Create();

        var result = await service.StartGameAsync("user-1", "Victor", "ZZ");

        Assert.Equal(StartGameStatus.InvalidCountry, result.Status);
        Assert.Empty(games.Items);
    }

    [Fact]
    public async Task Get_Owner_ReturnsTheGame()
    {
        var (service, _) = Create();
        var started = await service.StartGameAsync("user-1", "Victor", "C07");

        var game = await service.GetAsync("user-1", started.Game!.Id);

        Assert.NotNull(game);
    }

    [Fact]
    public async Task Get_AnotherUser_ReturnsNull()
    {
        var (service, _) = Create();
        var started = await service.StartGameAsync("user-1", "Victor", "C07");

        Assert.Null(await service.GetAsync("user-2", started.Game!.Id));
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNull()
    {
        var (service, _) = Create();
        Assert.Null(await service.GetAsync("user-1", Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task List_ReturnsOnlyTheUsersGames_NewestFirst()
    {
        var (service, games) = Create();
        games.Items.Add(new Game { Id = "old", OwnerUserId = "user-1", CreatedAt = new DateTime(2026, 1, 1) });
        games.Items.Add(new Game { Id = "new", OwnerUserId = "user-1", CreatedAt = new DateTime(2026, 6, 1) });
        games.Items.Add(new Game { Id = "other", OwnerUserId = "user-2", CreatedAt = new DateTime(2026, 9, 1) });

        var list = await service.ListAsync("user-1");

        Assert.Equal(new[] { "new", "old" }, list.Select(g => g.Id));
    }
}