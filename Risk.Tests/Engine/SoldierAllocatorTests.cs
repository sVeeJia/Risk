using Risk.Api.Engine;
using Xunit;

namespace Risk.Tests.Engine;

public class SoldierAllocatorTests
{
    public static IEnumerable<object[]> Cases => new List<object[]>
    {
        new object[] { 0, 1 },
        new object[] { 5_000_000, 1 },
        new object[] { 5_000_001, 2 },
        new object[] { 10_000_000, 2 },
        new object[] { 10_000_001, 3 },
        new object[] { 25_000_000, 3 },
        new object[] { 25_000_001, 4 },
        new object[] { 50_000_000, 4 },
        new object[] { 50_000_001, 5 },
        new object[] { 75_000_000, 5 },
        new object[] { 75_000_001, 6 },
        new object[] { 100_000_000, 6 },
        new object[] { 100_000_001, 7 },
        new object[] { 125_000_000, 7 },
        new object[] { 125_000_001, 8 },
        new object[] { 150_000_000, 8 },
        new object[] { 150_000_001, 9 },
        new object[] { 1_400_000_000, 9 },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void GetSoldiers_FollowsTheBriefsTable(long population, int expected)
    {
        Assert.Equal(expected, SoldierAllocator.GetSoldiers(population));
    }

    [Fact]
    public void GetSoldiers_NegativePopulation_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SoldierAllocator.GetSoldiers(-1));
    }
}