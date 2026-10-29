using Risk.Api.DesignPatterns.State.Interfaces;

namespace Risk.Api.DesignPatterns.State;

public sealed class FinishedState : IGamePhaseState
{
    public string? CheckCanStartAttack() => "The game is over.";
    public string? CheckCanRoll() => "The game is over.";
    public string? CheckCanStopAttack() => "The game is over.";
    public string? CheckCanTransferAfterConquest() => "The game is over.";
}