using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Strategy.Interfaces;

// How an NPC plays. Every decision comes with a justification the player can read.
public interface INpcStrategy
{
    // Where to place the player's pending reinforcements.
    IReadOnlyList<NpcPlacement> PlanReinforcements(Game game, Player player);

    // Which attack to start, or null to skip the attack this turn.
    NpcAttackChoice? ChooseAttack(Game game, Player player);

    // Whether to keep rolling in the attack that is running.
    NpcContinueDecision DecideContinue(Game game, Player player);

    // How many soldiers to move into the country that was just conquered.
    NpcTransferChoice ChooseConquestTransfer(Game game, Player player);

    // An optional move between two owned countries, or null for none.
    NpcTransferChoice? ChooseTransfer(Game game, Player player);
}