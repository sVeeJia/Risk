namespace Risk.Api.Dtos.Responses;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);