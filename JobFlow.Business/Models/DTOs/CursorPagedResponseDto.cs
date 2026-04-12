namespace JobFlow.Business.Models.DTOs;

public class CursorPagedResponseDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public string? NextCursor { get; set; }
    public int? TotalCount { get; set; }
    public int? WithEmailCount { get; set; }
    public int? WithPhoneCount { get; set; }
}
