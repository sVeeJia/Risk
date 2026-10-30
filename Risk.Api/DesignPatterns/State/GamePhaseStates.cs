using Risk.Api.DesignPatterns.State.Interfaces;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.State;

public static class GamePhaseStates
{
    private static readonly ReinforcingState Reinforcing = new();
    private static readonly ActionState Action = new();
    private static readonly AttackingState Attacking = new();
    private static readonly ConquestTransferState ConquestTransfer = new();
    private static readonly FinishedState Finished = new();

    public static IGamePhaseState For(GamePhase phase) => phase switch
    {
        GamePhase.Reinforcing => Reinforcing,
        GamePhase.Action => Action,
        GamePhase.Attacking => Attacking,
        GamePhase.ConquestTransfer => ConquestTransfer,
        GamePhase.Finished => Finished,
        _ => throw new ArgumentOutOfRangeException(nameof(phase))
    };
}