namespace Risk.Api.Dtos.Responses;

public sealed record GameEventResponse(
    int Sequence,
    string Type,
    string? PlayerId,
    string Message,
    string? Justification,
    IReadOnlyDictionary<string, string> Data,
    DateTime Timestamp);