using Risk.Api.Models;
using Risk.Api.Services;
using Risk.Api.Services.Interfaces;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;

namespace Risk.Tests.Services;

public class GameplayTurnFlowTests
{
    private const string UserId = "user-1";

    private readonly FakeGameRepository _games = new();
    private readonly FakeGameEventRepository _events = new();
    private readonly GameplayService _service;

    public GameplayTurnFlowTests() => _service = new GameplayService(_games, _events);

    private async Task<Game> SavedGame(Action<Game>? change = null)
    {
        var game = TestGames.HumanFirst();
        game.OwnerUserId = UserId;
        change?.Invoke(game);
        await _games.CreateAsync(game);
        return game;
    }

    [Fact]
    public async Task EndTurnAsync_PassesTheTurn_AndSavesTheEvents()
    {
        var game = await SavedGame();

        var result = await _service.EndTurnAsync(UserId, game.Id);

        Assert.Equal(GameplayStatus.Success, result.Status);
        Assert.NotEqual(game.Human().Id, result.Game!.TurnQueue.Peek());
        Assert.Equal(1, await _events.GetLastSequenceAsync(game.Id));
    }

    [Fact]
    public async Task PlaceReinforcementsAsync_AddsSoldiers()
    {
        var game = await SavedGame(g =>
        {
            g.Round = 2;
            g.Phase = GamePhase.Reinforcing;
            g.Human().PendingReinforcements = 2;
        });
        var home = game.Human().StartingCountryId;

        var result = await _service.PlaceReinforcementsAsync(UserId, game.Id, home, 2);

        Assert.Equal(GameplayStatus.Success, result.Status);
        Assert.Equal(7, result.Game!.Territories[home].Soldiers);
        Assert.Equal(GamePhase.Action, result.Game.Phase);
    }

    [Fact]
    public async Task TransferAsync_MovesSoldiers()
    {
        var game = await SavedGame(g =>
        {
            var npc = g.Players.First(p => !p.IsHuman);
            g.Human().OwnedCountryIds.UnionWith(npc.OwnedCountryIds);
            npc.OwnedCountryIds.Clear();
        });
        var home = game.Human().StartingCountryId;
        var second = game.EnemyCountry();

        var result = await _service.TransferAsync(UserId, game.Id, home, second, 2);

        Assert.Equal(GameplayStatus.Success, result.Status);
        Assert.Equal(3, result.Game!.Territories[home].Soldiers);
    }

    [Fact]
    public async Task EndTurnAsync_NotThePlayersTurn_IsRejected_AndNothingIsSaved()
    {
        var game = await SavedGame(g => g.GiveTurnToAnNpc());

        var result = await _service.EndTurnAsync(UserId, game.Id);

        Assert.Equal(GameplayStatus.Rejected, result.Status);
        Assert.Equal(0, await _events.GetLastSequenceAsync(game.Id));
    }

    [Fact]
    public async Task EndTurnAsync_ForSomeoneElsesGame_IsNotFound()
    {
        var game = await SavedGame();

        var result = await _service.EndTurnAsync("another-user", game.Id);

        Assert.Equal(GameplayStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task PlaceReinforcementsAsync_WithNothingToPlace_IsRejected()
    {
        var game = await SavedGame();

        var result = await _service.PlaceReinforcementsAsync(UserId, game.Id, game.Human().StartingCountryId, 1);

        Assert.Equal(GameplayStatus.Rejected, result.Status);
    }
    
    [Fact]
    public async Task EndTurnAsync_WhenTheGameChangedMeanwhile_ReturnsConflict_AndSavesNoEvents()
    {
        var game = await SavedGame();
        _games.FailNextUpdate = true;

        var result = await _service.EndTurnAsync(UserId, game.Id);

        Assert.Equal(GameplayStatus.Conflict, result.Status);
        Assert.Equal(0, await _events.GetLastSequenceAsync(game.Id));
    }

    [Fact]
    public async Task TransferAsync_WhenTheGameChangedMeanwhile_ReturnsConflict()
    {
        var game = await SavedGame(g =>
        {
            var npc = g.Players.First(p => !p.IsHuman);
            g.Human().OwnedCountryIds.UnionWith(npc.OwnedCountryIds);
            npc.OwnedCountryIds.Clear();
        });
        _games.FailNextUpdate = true;

        var result = await _service.TransferAsync(UserId, game.Id, game.Human().StartingCountryId, game.EnemyCountry(), 1);

        Assert.Equal(GameplayStatus.Conflict, result.Status);
    }
}