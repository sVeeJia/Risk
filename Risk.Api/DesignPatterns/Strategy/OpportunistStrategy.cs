using Risk.Api.DesignPatterns.Strategy.Interfaces;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Strategy;

// Attacks when the odds are good, takes the target that scores best, and gives up when the odds turn bad.
public sealed class OpportunistStrategy : INpcStrategy
{
    private const double MinWinChanceToStart = 0.5;
    private const double MinWinChanceToContinue = 0.35;

    private const double WinChanceWeight = 0.6;
    private const double RewardWeight = 0.2;
    private const double EliminationWeight = 0.1;
    private const double VictoryWeight = 0.3;
    private const double BiggestReward = 4; // 9 soldiers / 2, rounded down

    private const int MinGapToBalance = 3;

    private sealed record Candidate(
        Territory From,
        Territory To,
        int Attackers,
        double WinChance,
        int Reward,
        bool Eliminates,
        bool WinsGame,
        double Score);

    public NpcAttackChoice? ChooseAttack(Game game, Player player)
    {
        var best = Best(Candidates(game, player, null, 0), MinWinChanceToStart);
        if (best is null) return null;

        return new NpcAttackChoice(best.From.CountryId, best.To.CountryId, best.WinChance, Describe(best));
    }

    public IReadOnlyList<NpcPlacement> PlanReinforcements(Game game, Player player)
    {
        var pending = player.PendingReinforcements;
        if (pending <= 0) return Array.Empty<NpcPlacement>();

        var owned = OwnedTerritories(game, player);

        // For each country: how good would its best attack be with every reinforcement placed there?
        var options = owned
            .Select(country => Best(
                Candidates(game, player, country.CountryId, pending).Where(c => c.From.CountryId == country.CountryId),
                0))
            .Where(c => c is not null)
            .Select(c => c!)
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.From.CountryId)
            .ToList();

        if (options.Count > 0)
        {
            var pick = options[0];
            return new[]
            {
                new NpcPlacement(
                    pick.From.CountryId,
                    pending,
                    $"Places {pending} reinforcements in {pick.From.Name} ({pick.From.Soldiers} -> {pick.Attackers}): " +
                    $"that gives a {Percent(pick.WinChance)} chance to conquer {pick.To.Name}.")
            };
        }

        // No attack is possible: keep the soldiers in the strongest country.
        var strongest = owned.OrderByDescending(t => t.Soldiers).ThenBy(t => t.CountryId).First();
        return new[]
        {
            new NpcPlacement(
                strongest.CountryId,
                pending,
                $"Places {pending} reinforcements in {strongest.Name}, its strongest country.")
        };
    }

    public NpcContinueDecision DecideContinue(Game game, Player player)
    {
        var attack = game.ActiveAttack;
        if (attack is null) return new NpcContinueDecision(false, "There is no attack in progress.");

        var from = game.Territories[attack.AttackerCountryId];
        var to = game.Territories[attack.DefenderCountryId];
        var chance = WinProbability.Conquer(from.Soldiers, to.Soldiers);

        return chance >= MinWinChanceToContinue
            ? new NpcContinueDecision(true,
                $"Keeps attacking: still a {Percent(chance)} chance to conquer {to.Name} ({from.Soldiers} vs {to.Soldiers} soldiers).")
            : new NpcContinueDecision(false,
                $"Stops the attack: only a {Percent(chance)} chance left to conquer {to.Name} ({from.Soldiers} vs {to.Soldiers} soldiers).");
    }

    public NpcTransferChoice ChooseConquestTransfer(Game game, Player player)
    {
        var attack = game.ActiveAttack
                     ?? throw new InvalidOperationException("There is no conquered country to move soldiers into.");

        var from = game.Territories[attack.AttackerCountryId];
        var to = game.Territories[attack.DefenderCountryId];

        // Half the soldiers move in, but at least 1, and the attacking country always keeps 1.
        var soldiers = Math.Max(1, Math.Min(from.Soldiers - 1, from.Soldiers / 2));

        return new NpcTransferChoice(
            from.CountryId,
            to.CountryId,
            soldiers,
            $"Moves {soldiers} soldiers into {to.Name} so it is not left undefended, keeping {from.Soldiers - soldiers} in {from.Name}.");
    }

    public NpcTransferChoice? ChooseTransfer(Game game, Player player)
    {
        var owned = OwnedTerritories(game, player);
        if (owned.Count < 2) return null;

        var strongest = owned.OrderByDescending(t => t.Soldiers).ThenBy(t => t.CountryId).First();
        var weakest = owned.OrderBy(t => t.Soldiers).ThenBy(t => t.CountryId).First();

        var gap = strongest.Soldiers - weakest.Soldiers;
        if (gap < MinGapToBalance) return null;

        var soldiers = Math.Min(gap / 2, strongest.Soldiers - 1);

        return new NpcTransferChoice(
            strongest.CountryId,
            weakest.CountryId,
            soldiers,
            $"Moves {soldiers} soldiers from {strongest.Name} ({strongest.Soldiers}) to {weakest.Name} ({weakest.Soldiers}) to even out its defences.");
    }

    // Every attack the player could start, scored. A country can be boosted to see what reinforcements would do.
    private static IEnumerable<Candidate> Candidates(Game game, Player player, string? boostedCountryId, int boost)
    {
        var owned = OwnedTerritories(game, player);
        var enemies = game.Territories.Values.Where(t => !player.OwnedCountryIds.Contains(t.CountryId)).ToList();

        foreach (var from in owned)
        {
            var attackers = from.Soldiers + (from.CountryId == boostedCountryId ? boost : 0);
            if (attackers < 2) continue;

            foreach (var to in enemies)
            {
                var winChance = WinProbability.Conquer(attackers, to.Soldiers);
                var reward = BattleEngine.GetConquestReward(to.BaseSoldiers);
                var owner = game.Players.FirstOrDefault(p => p.OwnedCountryIds.Contains(to.CountryId));
                var eliminates = owner is not null && owner.OwnedCountryIds.Count == 1;
                var winsGame = enemies.Count == 1;

                var score = WinChanceWeight * winChance
                            + RewardWeight * (reward / BiggestReward)
                            + (eliminates ? EliminationWeight : 0)
                            + (winsGame ? VictoryWeight : 0);

                yield return new Candidate(from, to, attackers, winChance, reward, eliminates, winsGame, score);
            }
        }
    }

    // The best candidate with at least the given win chance, using a priority queue (best score first).
    private static Candidate? Best(IEnumerable<Candidate> candidates, double minWinChance)
    {
        var queue = new PriorityQueue<Candidate, (double, string, string)>();
        foreach (var candidate in candidates.Where(c => c.WinChance >= minWinChance))
        {
            // Lowest priority comes out first, so the score is negated. Ids break ties the same way every time.
            queue.Enqueue(candidate, (-candidate.Score, candidate.From.CountryId, candidate.To.CountryId));
        }

        return queue.TryDequeue(out var best, out _) ? best : null;
    }

    private static List<Territory> OwnedTerritories(Game game, Player player) =>
        game.Territories.Values.Where(t => player.OwnedCountryIds.Contains(t.CountryId)).ToList();

    private static string Describe(Candidate c)
    {
        var text = $"{Percent(c.WinChance)} chance to conquer {c.To.Name} ({c.Attackers} vs {c.To.Soldiers} soldiers), worth {c.Reward} new soldiers";
        if (c.WinsGame) return text + ", and it would win the game.";
        if (c.Eliminates) return text + ", and it would eliminate its owner.";
        return text + ".";
    }

    private static string Percent(double chance) => $"{Math.Round(chance * 100):0}%";
}