namespace Risk.Api.Models;

public class Territory
{
    public string CountryId { get; set; } = string.Empty; // ISO code
    public string Name { get; set; } = string.Empty;
    public int BaseSoldiers { get; set; } // from the population table; used for conquest bonus
    public int Soldiers { get; set; } // current soldiers
}