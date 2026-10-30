namespace Risk.Api.Models;

public enum EventType
{
    GameStarted,
    TurnStarted,
    ReinforcementsReceived,
    ReinforcementsPlaced,
    Transfer,
    AttackStarted,
    BattleRound, // dice rolled and soldiers lost
    CountryConquered,
    AttackEnded,
    TurnSkipped,
    Commentary,
    PlayerEliminated,
    GameWon,
    NpcDecision,
    GameLost
}