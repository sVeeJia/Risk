using Risk.Api.DesignPatterns.State.Interfaces;

namespace Risk.Api.DesignPatterns.State;

public sealed class FinishedState : IGamePhaseState
{
    private const string GameOver = "The game is over.";

    public string? CheckCanStartAttack() => GameOver;
    public string? CheckCanRoll() => GameOver;
    public string? CheckCanStopAttack() => GameOver;
    public string? CheckCanTransferAfterConquest() => GameOver;
    public string? CheckCanPlaceReinforcements() => GameOver;
    public string? CheckCanTransfer() => GameOver;
    public string? CheckCanEndTurn() => GameOver;
}