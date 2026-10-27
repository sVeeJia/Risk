using System.ComponentModel.DataAnnotations;

namespace Risk.Api.Dtos.Requests;

public sealed class StartGame
{
    [Required]
    [RegularExpression("^[A-Za-z]{2}$")]
    public string StartingCountryId { get; init; } = string.Empty;
}