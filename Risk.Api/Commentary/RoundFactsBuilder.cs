using System.Globalization;
using System.Text.RegularExpressions;
using Risk.Api.Models;

namespace Risk.Api.Commentary;

// Turns every BattleRound event in a list of events into RoundFacts.
public static class RoundFactsBuilder
{
    public static IReadOnlyList<RoundFacts> Build(Game game, IReadOnlyList<GameEvent> events)
    {
        var rounds = new List<RoundFacts>();

        for (var i = 0; i < events.Count; i++)
        {
            if (events[i].Type != EventType.BattleRound)
            {
                continue;
            }

            var next = i + 1 < events.Count ? events[i + 1] : null;
            var round = Describe(game, events[i], next);
            if (round is not null)
            {
                rounds.Add(round);
            }
        }

        return rounds;
    }

    // Keeps letters, digits and a few punctuation marks, on one line, and caps the length.
    public static string Clean(string? text, int maxLength = 40)
    {
        var kept = Regex.Replace(text ?? string.Empty, @"[^\p{L}\p{N} .,'()\-]", " ");
        kept = Regex.Replace(kept, @"\s+", " ").Trim();
        return kept.Length <= maxLength ? kept : kept[..maxLength].TrimEnd();
    }

    private static RoundFacts? Describe(Game game, GameEvent round, GameEvent? next)
    {
        // Rolls saved before the countries were recorded cannot be described, so they are skipped.
        var fromId = Value(round, "fromCountryId");
        var toId = Value(round, "toCountryId");
        if (fromId is null
            || toId is null
            || !game.Territories.TryGetValue(fromId, out var attacker)
            || !game.Territories.TryGetValue(toId, out var defender))
        {
            return null;
        }

        var playerId = round.PlayerId ?? string.Empty;

        return new RoundFacts(
            Sequence: round.Sequence,
            AttackerPlayerId: playerId,
            AttackerIsHuman: game.Players.FirstOrDefault(p => p.Id == playerId)?.IsHuman ?? false,
            AttackerCountryId: attacker.CountryId,
            AttackerCountry: Clean(attacker.Name),
            DefenderCountryId: defender.CountryId,
            DefenderCountry: Clean(defender.Name),
            AttackerDice: Clean(Value(round, "attackerDice")),
            DefenderDice: Clean(Value(round, "defenderDice")),
            AttackerLosses: Number(round, "attackerLosses"),
            DefenderLosses: Number(round, "defenderLosses"),
            AttackerSoldiers: Number(round, "attackerSoldiers"),
            DefenderSoldiers: Number(round, "defenderSoldiers"),
            Result: ResultOf(next));
    }

    // The event right after a roll tells what it led to.
    private static RoundResult ResultOf(GameEvent? next)
    {
        if (next is null) return RoundResult.Continues;
        if (next.Type == EventType.CountryConquered) return RoundResult.Conquered;
        if (next.Type == EventType.AttackEnded && Value(next, "reason") == "exhausted") return RoundResult.AttackerExhausted;

        return RoundResult.Continues;
    }

    private static string? Value(GameEvent e, string key) => e.Data is not null && e.Data.TryGetValue(key, out var value) ? value : null;

    private static int Number(GameEvent e, string key) => int.TryParse(Value(e, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : 0;
}