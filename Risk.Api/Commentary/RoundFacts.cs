namespace Risk.Api.Commentary;

// What the dice roll led to.
public enum RoundResult
{
    Continues,
    Conquered,
    AttackerExhausted
}

// The facts of one dice roll in a battle. 
public sealed record RoundFacts(
    int Sequence,
    string AttackerPlayerId,
    bool AttackerIsHuman,
    string AttackerCountryId,
    string AttackerCountry,
    string DefenderCountryId,
    string DefenderCountry,
    string AttackerDice,
    string DefenderDice,
    int AttackerLosses,
    int DefenderLosses,
    int AttackerSoldiers,
    int DefenderSoldiers,
    RoundResult Result);