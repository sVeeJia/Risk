using System.ComponentModel.DataAnnotations;

namespace Risk.Api.Dtos.Requests;

public sealed class StartAttack
{
    [Required]
    [RegularExpression("^[A-Za-z]{2}$")]
    public string FromCountryId { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^[A-Za-z]{2}$")]
    public string ToCountryId { get; init; } = string.Empty;
}