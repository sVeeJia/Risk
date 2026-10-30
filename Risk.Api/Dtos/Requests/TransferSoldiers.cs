using System.ComponentModel.DataAnnotations;

namespace Risk.Api.Dtos.Requests;

public sealed record TransferSoldiers(
    [property: Required, StringLength(2, MinimumLength = 2)] string FromCountryId,
    [property: Required, StringLength(2, MinimumLength = 2)] string ToCountryId,
    [property: Range(1, 1000)] int Soldiers);