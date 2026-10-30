namespace Risk.Api.Engine;

public sealed record BattleRound(
    IReadOnlyList<int> AttackerDice,
    IReadOnlyList<int> DefenderDice,
    int AttackerLosses,
    int DefenderLosses);

public enum BattleStatus
{
    Ongoing,
    Conquered,
    AttackerExhausted
}