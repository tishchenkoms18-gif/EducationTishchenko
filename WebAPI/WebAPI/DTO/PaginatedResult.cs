/// <summary>
/// Результат пагинации событий
/// </summary>
public class PaginatedResult<T>
{
    public int TotalCount { get; set; }
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}