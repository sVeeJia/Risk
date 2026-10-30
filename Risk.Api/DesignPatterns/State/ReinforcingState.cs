using Risk.Api.DesignPatterns.State.Interfaces;

namespace Risk.Api.DesignPatterns.State;

// From round 2 the player must place reinforcements before anything else.
public sealed class ReinforcingState : IGamePhaseState
{
    public string? CheckCanStartAttack() => "Place your reinforcements first.";
    public string? CheckCanRoll() => "There is no attack in progress.";
    public string? CheckCanStopAttack() => "There is no attack in progress.";
        public string? CheckCanTransferAfterConquest() => "There is no conquered country waiting for soldiers.";
}