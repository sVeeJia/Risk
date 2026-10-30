using Risk.Api.DesignPatterns.State.Interfaces;

namespace Risk.Api.DesignPatterns.State;

// The player's main phase: they may start an attack.
public sealed class ActionState : IGamePhaseState
{
    public string? CheckCanStartAttack() => null;
    public string? CheckCanRoll() => "There is no attack in progress.";
    public string? CheckCanStopAttack() => "There is no attack in progress.";
    public string? CheckCanTransferAfterConquest() => "There is no conquered country waiting for soldiers.";
}