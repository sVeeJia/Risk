using Risk.Api.Engine;

namespace Risk.Tests.Engine;

public class WinProbabilityTests
{
    [Fact]
    public void Conquer_AgainstNoDefenders_IsCertain() =>
        Assert.Equal(1, WinProbability.Conquer(3, 0));

    [Fact]
    public void Conquer_WithOneSoldier_IsImpossible() =>
        Assert.Equal(0, WinProbability.Conquer(1, 5));

    [Fact]
    public void Conquer_TwoAgainstOne_IsHalf() =>
        Assert.Equal(0.5, WinProbability.Conquer(2, 1), 9);

    [Fact]
    public void Conquer_ThreeAgainstOne_IsSeventyFivePercent() =>
        Assert.Equal(0.75, WinProbability.Conquer(3, 1), 9);

    [Fact]
    public void Conquer_TwoAgainstTwo_IsAQuarter() =>
        Assert.Equal(0.25, WinProbability.Conquer(2, 2), 9);

    [Fact]
    public void Conquer_MoreAttackersAlwaysHelps()
    {
        for (var attackers = 2; attackers < 12; attackers++)
        {
            Assert.True(WinProbability.Conquer(attackers + 1, 4) > WinProbability.Conquer(attackers, 4));
        }
    }

    [Fact]
    public void Conquer_MoreDefendersAlwaysHurts()
    {
        for (var defenders = 1; defenders < 8; defenders++)
        {
            Assert.True(WinProbability.Conquer(6, defenders + 1) < WinProbability.Conquer(6, defenders));
        }
    }

    [Fact]
    public void Conquer_IsAlwaysAProbability()
    {
        for (var attackers = 1; attackers <= 12; attackers++)
        {
            for (var defenders = 1; defenders <= 12; defenders++)
            {
                var chance = WinProbability.Conquer(attackers, defenders);
                Assert.InRange(chance, 0.0, 1.0);
            }
        }
    }
}