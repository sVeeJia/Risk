using System.ComponentModel.DataAnnotations;

namespace Risk.Api.Dtos.Requests;

public sealed record PlaceReinforcements(
    [Required, StringLength(2, MinimumLength = 2)] string CountryId,
    [Range(1, 1000)] int Soldiers);