namespace Comments.Application.Common;

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    // Rounds up: 26 items with 25 per page = 2 pages
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}
