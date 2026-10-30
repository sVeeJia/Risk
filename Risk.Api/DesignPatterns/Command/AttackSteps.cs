using Risk.Api.DataStructures;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Command;

internal static class AttackSteps
{
    // Ends the running attack and puts the game back in the action phase.
    public static void End(Game game, EventLog log, Player player, AttackEndReason reason)
    {
        game.ActiveAttack = null;
        game.Phase = GamePhase.Action;

        // The data value is a bare token for programs; the sentence (with its period) is for people.
        var token = reason.ToString().ToLowerInvariant();
        var text = reason switch
        {
            AttackEndReason.Conquest => "the country was conquered",
            AttackEndReason.Exhausted => "the attacker is down to 1 soldier",
            AttackEndReason.Stopped => "the attacker stopped",
            _ => throw new ArgumentOutOfRangeException(nameof(reason))
        };

        log.Append(
            EventType.AttackEnded,
            player.Id,
            $"{player.Name}'s attack ends: {text}.",
            data: new Dictionary<string, string> { ["reason"] = token });
    }
}