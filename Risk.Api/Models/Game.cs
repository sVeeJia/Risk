namespace Risk.Api.Models;

public class Game
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OwnerUserId { get; set; } = string.Empty;

    public GamePhase Phase { get; set; } = GamePhase.Action;
    public int Round { get; set; } = 1;

    public Dictionary<string, Territory> Territories { get; set; } = new(); // keyed by ISO code
    public List<Player> Players { get; set; } = new();
    public Queue<string> TurnQueue { get; set; } = new(); // player ids; front = current turn

    public AttackState? ActiveAttack { get; set; }
    public string? WinnerPlayerId { get; set; }

    public long RngSeed { get; set; } // never sent to viewers while in progress
    public long RngRollsUsed { get; set; } // lets the dice resume exactly where they left off

    public int Version { get; set; } // optimistic concurrency
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}