using Microsoft.Extensions.Logging.Abstractions;
using Risk.Api.DataStructures;
using Risk.Api.Dtos.Responses;
using Risk.Api.Realtime;
using Risk.Api.Models;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;

namespace Risk.Tests.Realtime;

public class SignalRGameObserverTests
{
    [Fact]
    public async Task OnGameChanged_SendsTheUpdateToThatGamesGroupOnly()
    {
        var hub = new FakeHubContext();
        var observer = new SignalRGameObserver(hub, NullLogger<SignalRGameObserver>.Instance);
        var game = TestGames.HumanFirst();
        var log = new EventLog(game.Id);
        log.Append(EventType.TurnStarted, game.Human().Id, "Your turn.");

        await observer.OnGameChangedAsync(game, log.Events);

        var sent = Assert.Single(hub.Sent);
        Assert.Equal($"game:{game.Id.ToLowerInvariant()}", sent.Group);
        Assert.Equal("GameUpdated", sent.Method);
        var update = Assert.IsType<ActionResponse>(Assert.Single(sent.Args));
        Assert.Equal(game.Id, update.Game.Id);
        Assert.Single(update.Events);
    }
}