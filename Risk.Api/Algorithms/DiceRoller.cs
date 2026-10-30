using Risk.Api.Algorithms.Interfaces;

namespace Risk.Api.Algorithms;

public static class DiceRoller
{
    // Rolls one six-sided die: 1 to 6.
    public static int RollDie(this IRandomSource random) => random.Next(6) + 1;
}