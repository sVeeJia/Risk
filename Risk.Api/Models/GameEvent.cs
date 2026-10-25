namespace Risk.Api.Models;

public class GameEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string GameId { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public EventType Type { get; set; }
    public string? PlayerId { get; set; }
    public string Message { get; set; } = string.Empty; // text shown in the UI or commentary
    public string? Justification { get; set; } // NPC reasoning, required for NpcDecision
    public Dictionary<string, string> Data { get; set; } = new(); // dice values, country ids, counts
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}