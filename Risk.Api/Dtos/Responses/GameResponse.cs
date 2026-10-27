namespace Risk.Api.Dtos.Responses;

public sealed record TerritoryResponse(
    string CountryId,
    string Name,
    int Slot,
    int BaseSoldiers,
    int Soldiers,
    string? OwnerPlayerId);

public sealed record PlayerResponse(
    string Id,
    string Name,
    bool IsHuman,
    string StartingCountryId,
    int CountriesOwned,
    bool IsEliminated);

public sealed record GameResponse(
    string Id,
    string Phase,
    int Round,
    string? CurrentPlayerId,
    string? WinnerPlayerId,
    IReadOnlyList<TerritoryResponse> Territories,
    IReadOnlyList<PlayerResponse> Players,
    DateTime CreatedAt);