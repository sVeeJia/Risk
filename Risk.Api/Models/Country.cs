namespace Risk.Api.Models;

public class Country
{
    public string Id { get; set; } = string.Empty; // ISO code, e.g. "ZA"
    public string Name { get; set; } = string.Empty;
    public long Population { get; set; }
    public string? FlagUrl { get; set; }
    public int Rank { get; set; }
}