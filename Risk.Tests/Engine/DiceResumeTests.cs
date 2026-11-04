using Risk.Api.Algorithms;

namespace Risk.Tests.Engine;

public class DiceResumeTests
{
    [Fact]
    public void ResumingAtASavedPosition_ContinuesTheSameSequence()
    {
        // A game saves its seed and how many numbers it has used. Loading it again resumes from that spot.
        var original = new SeededRandom(12345);
        for (var i = 0; i < 7; i++) original.RollDie();

        var resumed = new SeededRandom(12345, original.Position);

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(original.RollDie(), resumed.RollDie());
        }
    }

    [Fact]
    public void TwoGamesWithDifferentSeeds_RollDifferentSequences()
    {
        var a = new SeededRandom(1);
        var b = new SeededRandom(2);

        var first = Enumerable.Range(0, 30).Select(_ => a.RollDie()).ToList();
        var second = Enumerable.Range(0, 30).Select(_ => b.RollDie()).ToList();

        Assert.NotEqual(first, second);
    }
}