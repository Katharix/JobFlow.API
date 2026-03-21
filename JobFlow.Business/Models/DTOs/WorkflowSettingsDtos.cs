namespace JobFlow.Business.Models.DTOs;

public class WorkflowStatusDto
{
    public string StatusKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class WorkflowStatusUpsertRequestDto
{
    public string StatusKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
