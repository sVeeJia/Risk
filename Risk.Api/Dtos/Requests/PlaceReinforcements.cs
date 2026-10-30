using System.ComponentModel.DataAnnotations;

namespace Risk.Api.Dtos.Requests;

public sealed record PlaceReinforcements(
    [property: Required, StringLength(2, MinimumLength = 2)] string CountryId,
    [property: Range(1, 1000)] int Soldiers);