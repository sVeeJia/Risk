using Risk.Api.DesignPatterns.Strategy;
using Risk.Api.Services;
using Risk.Api.Services.Interfaces;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;
using Risk.Api.Models;

namespace Risk.Tests.Services;

public class GameplayObserverTests
{
    private const string UserId = "user-1";

    private readonly FakeGameRepository _games = new();
    private readonly FakeGameEventRepository _events = new();
    private readonly FakeGameObserver _observer = new();

    private GameplayService CreateService(params FakeGameObserver[] observers) =>
        new(_games, _events, new OpportunistStrategy(), observers);

    private async Task<Game> SavedGame(Action<Game>? change = null)
    {
        var game = TestGames.HumanFirst();
        game.OwnerUserId = UserId;
        change?.Invoke(game);
        await _games.CreateAsync(game);
        return game;
    }

    [Fact]
    public async Task ASuccessfulAction_NotifiesTheObserverWithTheNewEvents()
    {
        var game = await SavedGame();

        var result = await CreateService(_observer).EndTurnAsync(UserId, game.Id);

        var call = Assert.Single(_observer.Calls);
        Assert.Equal(game.Id, call.Game.Id);
        Assert.Equal(result.Events!.Count, call.Events.Count);
        Assert.NotEmpty(call.Events);
    }

    [Fact]
    public async Task EveryObserverIsNotified()
    {
        var game = await SavedGame();
        var second = new FakeGameObserver();

        await CreateService(_observer, second).EndTurnAsync(UserId, game.Id);

        Assert.Single(_observer.Calls);
        Assert.Single(second.Calls);
    }

    [Fact]
    public async Task ARejectedAction_NotifiesNobody()
    {
        var game = await SavedGame(g => g.GiveTurnToAnNpc());

        var result = await CreateService(_observer).EndTurnAsync(UserId, game.Id);

        Assert.Equal(GameplayStatus.Rejected, result.Status);
        Assert.Empty(_observer.Calls);
    }

    [Fact]
    public async Task AConflict_NotifiesNobody()
    {
        var game = await SavedGame();
        _games.FailNextUpdate = true;

        var result = await CreateService(_observer).EndTurnAsync(UserId, game.Id);

        Assert.Equal(GameplayStatus.Conflict, result.Status);
        Assert.Empty(_observer.Calls);
    }

    [Fact]
    public async Task SomeoneElsesGame_NotifiesNobody()
    {
        var game = await SavedGame();

        var result = await CreateService(_observer).EndTurnAsync("another-user", game.Id);

        Assert.Equal(GameplayStatus.NotFound, result.Status);
        Assert.Empty(_observer.Calls);
    }

    [Fact]
    public async Task AFailingObserver_DoesNotFailTheAction_AndTheOthersAreStillNotified()
    {
        var game = await SavedGame();
        var broken = new FakeGameObserver { Throw = true };

        var result = await CreateService(broken, _observer).EndTurnAsync(UserId, game.Id);

        Assert.Equal(GameplayStatus.Success, result.Status);
        Assert.Single(_observer.Calls);
    }
}