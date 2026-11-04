using Risk.Api.DesignPatterns.Strategy;
using Risk.Api.Models;
using Risk.Api.Services;
using Risk.Api.Services.Interfaces;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;

namespace Risk.Tests.Services;

public class GameplayCommentaryTests
{
    private const string UserId = "user-1";
    private readonly FakeGameRepository _games = new();
    private readonly FakeGameEventRepository _events = new();
    private readonly FakeGameObserver _observer = new();
    private readonly FakeCommentaryService _commentary = new();

    private GameplayService CreateService() => new(_games, _events, new OpportunistStrategy(), new[] { _observer }, _commentary);

    private async Task<Game> SavedGame(Action<Game>? change = null)
    {
        var game = TestGames.HumanFirst();
        game.OwnerUserId = UserId;
        change?.Invoke(game);
        await _games.CreateAsync(game);
        return game;
    }

    [Fact]
    public async Task ASuccessfulAction_StoresAndReturnsTheCommentary_AndTellsTheObservers()
    {
        var game = await SavedGame();

        var result = await CreateService().EndTurnAsync(UserId, game.Id);

        Assert.Equal(GameplayStatus.Success, result.Status);
        Assert.Contains(result.Events!, e => e.Type == EventType.Commentary);
        Assert.Contains(_events.Items, e => e.Type == EventType.Commentary);
        Assert.Equal(_events.Items.Count, _events.Items.Select(e => e.Sequence).Distinct().Count());
        Assert.Contains(Assert.Single(_observer.Calls).Events, e => e.Type == EventType.Commentary);
    }

    [Fact]
    public async Task AFailingCommentaryService_DoesNotFailTheAction()
    {
        _commentary.Throw = true;
        var game = await SavedGame();

        var result = await CreateService().EndTurnAsync(UserId, game.Id);

        Assert.Equal(GameplayStatus.Success, result.Status);
        Assert.DoesNotContain(_events.Items, e => e.Type == EventType.Commentary);
    }

    [Fact]
    public async Task ARejectedAction_DoesNotAskForCommentary()
    {
        var game = await SavedGame(g => g.GiveTurnToAnNpc());

        var result = await CreateService().EndTurnAsync(UserId, game.Id);

        Assert.Equal(GameplayStatus.Rejected, result.Status);
        Assert.Equal(0, _commentary.Calls);
    }
}