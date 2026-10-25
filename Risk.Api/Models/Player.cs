namespace Risk.Api.Models;

public class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public bool IsHuman { get; set; }
    public string StartingCountryId { get; set; } = string.Empty;
    public HashSet<string> OwnedCountryIds { get; set; } = new(); // single source of truth for ownership
    public int PendingReinforcements { get; set; }
    public bool IsEliminated { get; set; }
}