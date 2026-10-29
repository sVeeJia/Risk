using Risk.Api.DesignPatterns.State.Interfaces;

namespace Risk.Api.DesignPatterns.State;

// An attack is running: the player may roll again or stop.
public sealed class AttackingState : IGamePhaseState
{
    public string? CheckCanStartAttack() => "An attack is already in progress.";
    public string? CheckCanRoll() => null;
    public string? CheckCanStopAttack() => null;
        public string? CheckCanTransferAfterConquest() => "There is no conquered country waiting for soldiers.";
}