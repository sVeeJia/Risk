using Risk.Api.DesignPatterns.State.Interfaces;

namespace Risk.Api.DesignPatterns.State;

// An attack is running: the player may roll again or stop, and nothing else.
public sealed class AttackingState : IGamePhaseState
{
    public string? CheckCanStartAttack() => "An attack is already in progress.";
    public string? CheckCanRoll() => null;
    public string? CheckCanStopAttack() => null;
    public string? CheckCanTransferAfterConquest() => "There is no conquered country waiting for soldiers.";
    public string? CheckCanPlaceReinforcements() => "You have no reinforcements to place.";
    public string? CheckCanTransfer() => "You cannot move soldiers while an attack is in progress.";
    public string? CheckCanEndTurn() => "Finish or stop your attack first.";
}