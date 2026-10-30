using Risk.Api.DesignPatterns.State.Interfaces;

namespace Risk.Api.DesignPatterns.State;

// The player's main phase: they may attack, move soldiers between their countries, or end the turn.
public sealed class ActionState : IGamePhaseState
{
    public string? CheckCanStartAttack() => null;
    public string? CheckCanRoll() => "There is no attack in progress.";
    public string? CheckCanStopAttack() => "There is no attack in progress.";
    public string? CheckCanTransferAfterConquest() => "There is no conquered country waiting for soldiers.";
    public string? CheckCanPlaceReinforcements() => "You have no reinforcements to place.";
    public string? CheckCanTransfer() => null;
    public string? CheckCanEndTurn() => null;
}