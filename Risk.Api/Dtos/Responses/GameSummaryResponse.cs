namespace Risk.Api.Dtos.Responses;

public sealed record GameSummaryResponse(
    string Id,
    string Phase,
    int Round,
    string YourCountryId,
    string YourCountryName,
    DateTime CreatedAt);