using Risk.Api.DataStructures;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Command;

internal static class AttackSteps
{
    // Ends the running attack and puts the game back in the action phase.
    public static void End(Game game, EventLog log, Player player, string reason)
    {
        game.ActiveAttack = null;
        game.Phase = GamePhase.Action;

        log.Append(
            EventType.AttackEnded,
            player.Id,
            $"{player.Name}'s attack ends: {reason}",
            data: new Dictionary<string, string> { ["reason"] = reason });
    }
}