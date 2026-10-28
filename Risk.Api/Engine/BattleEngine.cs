using Risk.Api.Algorithms;
using Risk.Api.Algorithms.Interfaces;

namespace Risk.Api.Engine;

public static class BattleEngine
{
    // Resolves one round of an attack: roll, pair the dice, work out the losses.
    public static BattleRound ResolveRound(int attackerSoldiers, int defenderSoldiers, IRandomSource random)
    {
        if (attackerSoldiers < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(attackerSoldiers), "An attacker needs at least 2 soldiers.");
           
        }

        if (defenderSoldiers < 1) 
        {
            throw new ArgumentOutOfRangeException(nameof(defenderSoldiers), "The defender has no soldiers left.");
        }

        // One die per soldier, up to what both sides can field. One attacker always stays behind.
        var diceCount = Math.Min(attackerSoldiers - 1, defenderSoldiers);

        // The attacker rolls first, then the defender (keeps replays identical).
        var attackerDice = RollDice(diceCount, random);
        var defenderDice = RollDice(diceCount, random);

        var attackerLosses = 0;
        var defenderLosses = 0;

        // Die 1 against Die 1, Die 2 against Die 2, and so on (not sorted).
        // The higher die wins and the loser loses a soldier. A tie costs nobody anything.
        for (var i = 0; i < diceCount; i++)
        {
            if (attackerDice[i] > defenderDice[i]) {
                defenderLosses++;
            }
            else if (defenderDice[i] > attackerDice[i]) {
                attackerLosses++;
            }
        }

        return new BattleRound(attackerDice, defenderDice, attackerLosses, defenderLosses);
    }

    // Where the attack stands after a round.
    public static BattleStatus GetStatus(int attackerSoldiers, int defenderSoldiers)
    {
        if (defenderSoldiers <= 0) {
            return BattleStatus.Conquered;
        }

        if (attackerSoldiers <= 1) {
            return BattleStatus.AttackerExhausted;
        }

        return BattleStatus.Ongoing;
    }

    // Soldiers gained for conquering a country. Uses the country's BASE soldiers (its population value), not how many it had left when it fell.
    public static int GetConquestReward(int defenderBaseSoldiers) => Math.Max(2, defenderBaseSoldiers / 2);

    private static int[] RollDice(int count, IRandomSource random)
    {
        var dice = new int[count];

        for (var i = 0; i < count; i++) 
        {
            dice[i] = random.RollDie();
        }

        return dice;
    }
}