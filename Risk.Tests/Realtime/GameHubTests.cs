using Microsoft.AspNetCore.SignalR;
using Risk.Api.Realtime;
using Risk.Api.Services;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;

namespace Risk.Tests.Realtime;

public class GameHubTests
{
    private readonly FakeGameRepository _games = new();
    private readonly FakeGameEventRepository _events = new();
    private readonly FakeGroupManager _groups = new();

    private GameHub CreateHub() =>
        new(new PublicGameService(_games, _events))
        {
            Context = new FakeHubCallerContext("connection-1"),
            Groups = _groups
        };

    [Fact]
    public async Task Watch_AnExistingGame_JoinsThatGamesGroup()
    {
        var game = TestGames.HumanFirst();
        await _games.CreateAsync(game);

        await CreateHub().Watch(game.Id);

        var joined = Assert.Single(_groups.Added);
        Assert.Equal("connection-1", joined.ConnectionId);
        Assert.Equal($"game:{game.Id.ToLowerInvariant()}", joined.Group);
    }

    [Fact]
    public async Task Watch_AnUnknownGame_IsRefused()
    {
        await Assert.ThrowsAsync<HubException>(() => CreateHub().Watch(Guid.NewGuid().ToString()));

        Assert.Empty(_groups.Added);
    }

    [Fact]
    public async Task Watch_SomethingThatIsNotAGameId_IsRefused()
    {
        await Assert.ThrowsAsync<HubException>(() => CreateHub().Watch("not-a-guid"));

        Assert.Empty(_groups.Added);
    }

    [Fact]
    public async Task StopWatching_LeavesTheGroup()
    {
        var game = TestGames.HumanFirst();

        await CreateHub().StopWatching(game.Id);

        Assert.Equal($"game:{game.Id.ToLowerInvariant()}", Assert.Single(_groups.Removed).Group);
    }
}