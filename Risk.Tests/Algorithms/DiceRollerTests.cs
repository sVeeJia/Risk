using Risk.Api.Algorithms;

namespace Risk.Tests.Algorithms;

public class DiceRollerTests
{
    [Fact]
    public void RollDie_StaysBetweenOneAndSix_AndHitsEveryFaceAboutEquallyOften()
    {
        var random = new SeededRandom(99);
        var counts = new int[7];
        for (var i = 0; i < 6000; i++) counts[random.RollDie()]++;

        Assert.Equal(0, counts[0]);
        for (var face = 1; face <= 6; face++) Assert.InRange(counts[face], 800, 1200); // about 1000 each
    }
}