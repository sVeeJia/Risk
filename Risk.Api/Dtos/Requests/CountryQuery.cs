using System.ComponentModel.DataAnnotations;

namespace Risk.Api.Dtos.Requests;

public sealed class CountryQuery
{
    [MaxLength(50)] public string? Search { get; init; }
    [Range(1, 9)] public int? Soldiers { get; init; }
    [Range(0, long.MaxValue)] public long? MinPopulation { get; init; }
    [Range(0, long.MaxValue)] public long? MaxPopulation { get; init; }
    [RegularExpression("^(rank|name|population)$")] public string Sort { get; init; } = "rank";
    [RegularExpression("^(asc|desc)$")] public string Direction { get; init; } = "asc";
    [Range(1, 1000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 25;
}