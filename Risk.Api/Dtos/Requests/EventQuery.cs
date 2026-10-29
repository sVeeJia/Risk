using System.ComponentModel.DataAnnotations;

namespace Risk.Api.Dtos.Requests;

public sealed class EventQuery
{
    [Range(0, int.MaxValue)] public int After { get; init; } = 0;
    [Range(1, 500)] public int Limit { get; init; } = 200;
}