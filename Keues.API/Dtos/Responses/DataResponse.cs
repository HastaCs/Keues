namespace Keues.API.Responses;

public record DataResponse<T>(T Data, Pagination? Pagination = null);

public record Pagination(int Page, int Limit, int Total, int TotalPages);