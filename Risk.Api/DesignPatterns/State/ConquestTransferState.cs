using Risk.Api.DesignPatterns.State.Interfaces;

namespace Risk.Api.DesignPatterns.State;

// A country has just been conquered: the player must choose how many soldiers to move into it.
public sealed class ConquestTransferState : IGamePhaseState
{
    private const string MoveFirst = "Move soldiers into the conquered country first.";

    public string? CheckCanStartAttack() => MoveFirst;
    public string? CheckCanRoll() => MoveFirst;
    public string? CheckCanStopAttack() => MoveFirst;
    public string? CheckCanTransferAfterConquest() => null;
    public string? CheckCanPlaceReinforcements() => "You have no reinforcements to place.";
    public string? CheckCanTransfer() => MoveFirst;
    public string? CheckCanEndTurn() => MoveFirst;
}