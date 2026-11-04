using System.Collections.Concurrent;

namespace Risk.Api.Engine;

// The chance that an attacker keeps rolling until the defender has no soldiers left.
// Each pair of dice: attacker wins 15/36, defender wins 15/36, tie 6/36 (a tie kills nobody).
public static class WinProbability
{
    private const double AttackerWins = 15.0 / 36;
    private const double DefenderWins = 15.0 / 36;
    private const double Tie = 6.0 / 36;

    // Results only depend on the two soldier counts, so they are remembered between calls.
    private static readonly ConcurrentDictionary<(int, int), double> Known = new();

    public static double Conquer(int attackers, int defenders)
    {
        if (defenders <= 0) return 1;
        if (attackers <= 1) return 0;

        if (Known.TryGetValue((attackers, defenders), out var cached)) return cached;

        var pairs = Math.Min(attackers - 1, defenders);
        var total = 0.0;

        for (var defenderLosses = 0; defenderLosses <= pairs; defenderLosses++)
        {
            for (var attackerLosses = 0; attackerLosses + defenderLosses <= pairs; attackerLosses++)
            {
                // Nobody lost a soldier: the same round is simply rolled again.
                if (defenderLosses == 0 && attackerLosses == 0) continue;

                var ties = pairs - defenderLosses - attackerLosses;
                var chance = Arrangements(pairs, defenderLosses, attackerLosses)
                             * Math.Pow(AttackerWins, defenderLosses)
                             * Math.Pow(DefenderWins, attackerLosses)
                             * Math.Pow(Tie, ties);

                total += chance * Conquer(attackers - attackerLosses, defenders - defenderLosses);
            }
        }

        // Leave out the "nothing happened" outcome and scale the rest back up to 100%.
        var result = total / (1 - Math.Pow(Tie, pairs));
        Known[(attackers, defenders)] = result;
        return result;
    }

    // Ways to place x attacker wins and y defender wins among n pairs of dice.
    private static double Arrangements(int n, int x, int y) => Choose(n, x) * Choose(n - x, y);

    private static double Choose(int n, int k)
    {
        double result = 1;
        for (var i = 1; i <= k; i++)
        {
            result = result * (n - k + i) / i;
        }

        return result;
    }
}