using Risk.Api.Algorithms;

namespace Risk.Tests.Algorithms;

public class FisherYatesShuffleTests
{
    [Fact]
    public void Shuffle_KeepsTheSameElements()
    {
        var items = Enumerable.Range(1, 50).ToList();
        FisherYatesShuffle.Shuffle(items, new SeededRandom(5));
        Assert.Equal(Enumerable.Range(1, 50), items.OrderBy(x => x));
    }

    [Fact]
    public void SameSeed_GivesSameOrder()
    {
        var a = FisherYatesShuffle.Shuffled(Enumerable.Range(1, 30), new SeededRandom(11));
        var b = FisherYatesShuffle.Shuffled(Enumerable.Range(1, 30), new SeededRandom(11));
        Assert.Equal(a, b);
    }

    [Fact]
    public void Shuffled_DoesNotChangeTheSource()
    {
        var source = new[] { 1, 2, 3, 4, 5 };
        FisherYatesShuffle.Shuffled(source, new SeededRandom(3));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source);
    }

    [Fact]
    public void EmptyAndSingleItem_AreFine()
    {
        Assert.Empty(FisherYatesShuffle.Shuffled(Array.Empty<int>(), new SeededRandom(1)));
        Assert.Equal(new[] { 9 }, FisherYatesShuffle.Shuffled(new[] { 9 }, new SeededRandom(1)));
    }

    [Fact]
    public void AllSixOrderingsOfThreeItems_AppearAboutEquallyOften()
    {
        var counts = new Dictionary<string, int>();
        for (var seed = 0; seed < 6000; seed++)
        {
            var order = string.Join("", FisherYatesShuffle.Shuffled(new[] { 1, 2, 3 }, new SeededRandom(seed)));
            counts[order] = counts.GetValueOrDefault(order) + 1;
        }

        Assert.Equal(6, counts.Count);
        Assert.All(counts.Values, c => Assert.InRange(c, 800, 1200)); // about 1000 each
    }
}