using Risk.Api.Algorithms;

namespace Risk.Tests.Algorithms;

public class SeededRandomTests
{
    [Fact]
    public void SameSeed_GivesSameSequence()
    {
        var a = new SeededRandom(42);
        var b = new SeededRandom(42);
        for (var i = 0; i < 100; i++) Assert.Equal(a.Next(1000), b.Next(1000));
    }

    [Fact]
    public void DifferentSeeds_GiveDifferentSequences()
    {
        var a = new SeededRandom(1);
        var b = new SeededRandom(2);
        var same = Enumerable.Range(0, 50).Count(_ => a.Next(1_000_000) == b.Next(1_000_000));
        Assert.True(same < 5);
    }

    [Fact]
    public void ResumingFromPosition_ContinuesTheSameSequence()
    {
        var original = new SeededRandom(7);
        for (var i = 0; i < 25; i++) original.Next(100);
        var resumed = new SeededRandom(7, original.Position);

        for (var i = 0; i < 25; i++) Assert.Equal(original.Next(100), resumed.Next(100));
    }

    [Fact]
    public void Position_CountsDraws()
    {
        var rng = new SeededRandom(1);
        rng.Next(6); rng.Next(6); rng.Next(10);
        Assert.Equal(3, rng.Position);
    }

    [Fact]
    public void Next_StaysInRange()
    {
        var rng = new SeededRandom(99);
        for (var i = 0; i < 1000; i++) Assert.InRange(rng.Next(6), 0, 5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Next_NonPositiveMax_Throws(int max) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SeededRandom(1).Next(max));

    [Fact]
    public void NegativePosition_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SeededRandom(1, -1));
}