namespace Risk.Api.Models;

public enum GamePhase
{
    Reinforcing, // start of turn: placing new soldiers (round 2 onward)
    Action, // may transfer, attack, or skip
    Attacking, // an attack is in progress
    Finished,
    ConquestTransfer
}