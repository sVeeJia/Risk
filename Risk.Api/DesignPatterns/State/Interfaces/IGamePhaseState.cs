namespace Risk.Api.DesignPatterns.State.Interfaces;

public interface IGamePhaseState
{
    // Each check returns null when the action is allowed, or the reason it is not.
    string? CheckCanStartAttack();
    string? CheckCanRoll();
    string? CheckCanStopAttack();
    string? CheckCanTransferAfterConquest();
    string? CheckCanPlaceReinforcements();
    string? CheckCanTransfer();
    string? CheckCanEndTurn();
}