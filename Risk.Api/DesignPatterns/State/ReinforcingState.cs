using Risk.Api.DesignPatterns.State.Interfaces;

namespace Risk.Api.DesignPatterns.State;

// From round 2 the player must place all their reinforcements before anything else.
public sealed class ReinforcingState : IGamePhaseState
{
    private const string PlaceFirst = "Place your reinforcements first.";

    public string? CheckCanStartAttack() => PlaceFirst;
    public string? CheckCanRoll() => "There is no attack in progress.";
    public string? CheckCanStopAttack() => "There is no attack in progress.";
    public string? CheckCanTransferAfterConquest() => "There is no conquered country waiting for soldiers.";
    public string? CheckCanPlaceReinforcements() => null;
    public string? CheckCanTransfer() => PlaceFirst;
    public string? CheckCanEndTurn() => PlaceFirst;
}