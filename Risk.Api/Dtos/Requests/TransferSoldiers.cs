using System.ComponentModel.DataAnnotations;

namespace Risk.Api.Dtos.Requests;

public sealed record TransferSoldiers(
    [Required, StringLength(2, MinimumLength = 2)] string FromCountryId,
    [Required, StringLength(2, MinimumLength = 2)] string ToCountryId,
    [Range(1, 1000)] int Soldiers);