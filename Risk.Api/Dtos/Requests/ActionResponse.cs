namespace Risk.Api.Dtos.Responses;

// What every game action returns: the game as it is now, and the events that just happened.
public sealed record ActionResponse(GameResponse Game, IReadOnlyList<GameEventResponse> Events);