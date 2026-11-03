using Risk.Api.Commentary;
using Risk.Api.DataStructures;
using Risk.Api.Models;

namespace Risk.Tests.Helpers;

// Short ways to write the events of a battle into a log, shaped like the real ones.
public static class BattleLogs
{
    public static void Round(
        this EventLog log,
        string playerId,
        string from,
        string to,
        int attackerLosses = 1,
        int defenderLosses = 1,
        int attackerSoldiers = 4,
        int defenderSoldiers = 2,
        string attackerDice = "6,2",
        string defenderDice = "4,3") =>
        log.Append(EventType.BattleRound, playerId, "Dice are rolled.", data: new Dictionary<string, string>
        {
            ["fromCountryId"] = from,
            ["toCountryId"] = to,
            ["attackerDice"] = attackerDice,
            ["defenderDice"] = defenderDice,
            ["attackerLosses"] = attackerLosses.ToString(),
            ["defenderLosses"] = defenderLosses.ToString(),
            ["attackerSoldiers"] = attackerSoldiers.ToString(),
            ["defenderSoldiers"] = defenderSoldiers.ToString()
        });

    public static void Conquered(this EventLog log, string playerId, string countryId) =>
        log.Append(EventType.CountryConquered, playerId, "A country is conquered.", data: new Dictionary<string, string>
        {
            ["countryId"] = countryId,
            ["reward"] = "2",
            ["previousOwnerId"] = "previous-owner"
        });

    public static void Ended(this EventLog log, string playerId, string reason) =>
        log.Append(EventType.AttackEnded, playerId, "The attack ends.", data: new Dictionary<string, string> { ["reason"] = reason });
}

public static class RoundSamples
{
    public static RoundFacts Sample(
        RoundResult result = RoundResult.Continues,
        int attackerLosses = 1,
        int defenderLosses = 2,
        int sequence = 7) =>
        new(sequence, "player-1", true, "PE", "Peru", "JO", "Jordan", "6,2", "4,3", attackerLosses, defenderLosses, 4, 2, result);
}