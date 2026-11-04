using Risk.Api.Commentary;
using Risk.Api.DataStructures;
using Risk.Api.Models;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;

namespace Risk.Tests.Commentary;

public class CommentaryServiceTests
{
    private readonly FakeCommentaryGenerator _generator = new();
    private readonly Game _game = TestGames.HumanFirst();

    private CommentaryService Create() => new(_generator);
    private Player Human => _game.Human();
    private string Mine => Human.StartingCountryId;
    private string Enemy => _game.EnemyCountry();

    [Fact]
    public async Task ARoll_GetsACommentaryEventThatPointsAtIt()
    {
        var log = new EventLog(_game.Id);
        log.Round(Human.Id, Mine, Enemy);

        await Create().AddAsync(_game, log);

        var commentary = log.Events.Last();
        Assert.Equal(EventType.Commentary, commentary.Type);
        Assert.Equal("What a battle!", commentary.Message);
        Assert.Equal(Human.Id, commentary.PlayerId);
        Assert.Equal("1", commentary.Data["forSequence"]);
        Assert.Equal("llm", commentary.Data["source"]);
        Assert.Equal(Mine, commentary.Data["fromCountryId"]);
        Assert.Equal(Enemy, commentary.Data["toCountryId"]);
    }

    [Fact]
    public async Task EveryRoll_GetsItsOwnLine_InOrder()
    {
        var log = new EventLog(_game.Id);
        log.Round(Human.Id, Mine, Enemy);
        log.Round(Human.Id, Mine, Enemy);

        await Create().AddAsync(_game, log);

        var lines = log.Events.Where(e => e.Type == EventType.Commentary).ToList();
        Assert.Equal(new[] { "1", "2" }, lines.Select(e => e.Data["forSequence"]));
    }

    [Fact]
    public async Task WithNoRolls_DoesNothing()
    {
        var log = new EventLog(_game.Id);
        log.Append(EventType.TurnStarted, Human.Id, "A turn begins.");

        await Create().AddAsync(_game, log);

        Assert.Single(log.Events);
        Assert.Empty(_generator.Rounds);
    }

    [Fact]
    public async Task ARollByAComputerPlayer_GetsNoLine()
    {
        var npc = _game.Players.First(p => !p.IsHuman);
        var log = new EventLog(_game.Id);
        log.Round(npc.Id, npc.StartingCountryId, Mine);

        await Create().AddAsync(_game, log);

        Assert.DoesNotContain(log.Events, e => e.Type == EventType.Commentary);
        Assert.Empty(_generator.Rounds);
    }

    [Fact]
    public async Task WhenBothPlayTheSameRequest_OnlyTheHumansRollsGetALine()
    {
        var npc = _game.Players.First(p => !p.IsHuman);
        var log = new EventLog(_game.Id);
        log.Round(npc.Id, npc.StartingCountryId, Mine);
        log.Round(Human.Id, Mine, Enemy);

        await Create().AddAsync(_game, log);

        var line = Assert.Single(log.Events, e => e.Type == EventType.Commentary);
        Assert.Equal("2", line.Data["forSequence"]);
        Assert.Equal(Human.Id, line.PlayerId);
    }

    [Fact]
    public async Task ARollSavedBeforeCountriesWereRecorded_GetsNoLine()
    {
        var log = new EventLog(_game.Id);
        log.Append(EventType.BattleRound, Human.Id, "An old roll.");

        await Create().AddAsync(_game, log);

        Assert.DoesNotContain(log.Events, e => e.Type == EventType.Commentary);
    }
}