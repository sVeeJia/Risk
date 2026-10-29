using Risk.Api.Dtos.Responses;
using Risk.Api.Models;

namespace Risk.Api.Dtos.Mappers;

public static class GameEventMapper
{
    public static GameEventResponse ToResponse(GameEvent e) =>
        new(e.Sequence,
            e.Type.ToString(),
            e.PlayerId,
            e.Message,
            e.Justification,
            e.Data ?? new Dictionary<string, string>(),
            e.Timestamp);
}