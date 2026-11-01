using System.Text.Json;
using Risk.Api.DataStructures;
using Risk.Api.Dtos.Mappers;
using Risk.Api.Models;
using Risk.Api.Services;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;

namespace Risk.Tests.Services;

public class PublicGameServiceTests
{
    private readonly FakeGameRepository _games = new();
    private readonly FakeGameEventRepository _events = new();
    private readonly PublicGameService _service;

    public PublicGameServiceTests() => _service = new PublicGameService(_games, _events);

    private async Task<Game> SavedGame()
    {
        var game = TestGames.HumanFirst();
        game.OwnerUserId = "owner-secret-id";
        game.RngSeed = 987654321987654L;
        await _games.CreateAsync(game);
        return game;
    }

    private async Task AddEvents(Game game, int count)
    {
        var log = new EventLog(game.Id);
        for (var i = 1; i <= count; i++)
        {
            log.Append(EventType.TurnStarted, game.Human().Id, $"Event {i}");
        }

        await _events.AppendAsync(log.Events);
    }

    [Fact]
    public async Task GetGameAsync_ReturnsTheGameToAnyone()
    {
        var game = await SavedGame();

        var result = await _service.GetGameAsync(game.Id);

        Assert.Equal(game.Id, result!.Id);
    }

    [Fact]
    public async Task GetGameAsync_UnknownGame_ReturnsNull() =>
        Assert.Null(await _service.GetGameAsync(Guid.NewGuid().ToString()));

    [Fact]
    public async Task GetEventsAsync_ReturnsOnlyEventsAfterTheCursor()
    {
        var game = await SavedGame();
        await AddEvents(game, 3);

        var result = await _service.GetEventsAsync(game.Id, after: 1, limit: 50);

        Assert.Equal(new[] { 2, 3 }, result!.Select(e => e.Sequence));
    }

    [Fact]
    public async Task GetEventsAsync_RespectsTheLimit()
    {
        var game = await SavedGame();
        await AddEvents(game, 3);

        var result = await _service.GetEventsAsync(game.Id, after: 0, limit: 1);

        Assert.Single(result!);
    }

    [Fact]
    public async Task GetEventsAsync_UnknownGame_ReturnsNull() =>
        Assert.Null(await _service.GetEventsAsync(Guid.NewGuid().ToString(), 0, 50));

    [Fact]
    public async Task PublicView_NeverExposesTheSeedOrTheOwnerId()
    {
        var game = await SavedGame();

        var json = JsonSerializer.Serialize(GameMapper.ToResponse((await _service.GetGameAsync(game.Id))!));

        Assert.DoesNotContain("987654321987654", json);
        Assert.DoesNotContain("owner-secret-id", json);
    }
}