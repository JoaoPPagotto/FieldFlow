using System.Text.Json.Serialization;

namespace FieldFlow.Application.DTOs;

public class BaseDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore] public string? CreatedBy { get; set; }
    [JsonIgnore] public string? UpdatedBy { get; set; }
    [JsonIgnore] public bool IsDeleted { get; set; }
}

public class PagedRequestDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }

    /// <summary>Ex.: <c>-openedAt</c> (desc) ou <c>name</c> (asc).</summary>
    public string? Sort { get; set; }
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
