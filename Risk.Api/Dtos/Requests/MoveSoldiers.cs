using System.ComponentModel.DataAnnotations;

namespace Risk.Api.Dtos.Requests;

public sealed class MoveSoldiers
{
    [Range(1, 1000)]
    public int Soldiers { get; init; }
}