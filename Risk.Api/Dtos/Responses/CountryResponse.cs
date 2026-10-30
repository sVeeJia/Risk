namespace Risk.Api.Dtos.Responses;

public sealed record CountryResponse(string Id, string Name, long Population, int Rank, int Soldiers);