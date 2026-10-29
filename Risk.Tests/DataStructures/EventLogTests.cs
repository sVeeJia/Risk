using Risk.Api.DataStructures;
using Risk.Api.Models;

namespace Risk.Tests.DataStructures;

public class EventLogTests
{
    [Fact]
    public void Append_NumbersEventsOneTwoThree()
    {
        var log = new EventLog("game-1");

        log.Append(EventType.GameStarted, null, "start");
        log.Append(EventType.TurnStarted, "p1", "turn");
        log.Append(EventType.AttackStarted, "p1", "attack");

        Assert.Equal(new[] { 1, 2, 3 }, log.Events.Select(e => e.Sequence));
    }

    [Fact]
    public void NewLog_CanContinueAfterAnExistingSequence()
    {
        var log = new EventLog("game-1", lastSequence: 5);

        var next = log.Append(EventType.TurnStarted, "p1", "turn");

        Assert.Equal(6, next.Sequence);
    }

    [Fact]
    public void Since_ReturnsOnlyLaterEvents()
    {
        var log = new EventLog("game-1");
        log.Append(EventType.GameStarted, null, "one");
        log.Append(EventType.TurnStarted, "p1", "two");
        log.Append(EventType.AttackStarted, "p1", "three");

        Assert.Equal(new[] { 2, 3 }, log.Since(1).Select(e => e.Sequence));
        Assert.Empty(log.Since(3));
    }

    [Fact]
    public void Append_StoresTheDetails()
    {
        var log = new EventLog("game-7");

        var e = log.Append(
            EventType.NpcDecision, "p2", "Iceland attacks Congo",
            justification: "Highest win probability",
            data: new Dictionary<string, string> { ["target"] = "CG" });

        Assert.Equal("game-7", e.GameId);
        Assert.Equal(EventType.NpcDecision, e.Type);
        Assert.Equal("p2", e.PlayerId);
        Assert.Equal("Highest win probability", e.Justification);
        Assert.Equal("CG", e.Data["target"]);
        Assert.False(string.IsNullOrEmpty(e.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Append_BlankMessage_Throws(string message)
    {
        var log = new EventLog("game-1");
        Assert.Throws<ArgumentException>(() => log.Append(EventType.GameStarted, null, message));
    }
}