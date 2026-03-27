using JobFlow.Domain.Enums;

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

public class InvoicingSettingsDto
{
    public InvoicingWorkflow DefaultWorkflow { get; set; }
    public bool DepositRequired { get; set; }
    public decimal DepositPercentage { get; set; }
}

public class InvoicingSettingsUpsertRequestDto
{
    public InvoicingWorkflow DefaultWorkflow { get; set; }
    public bool DepositRequired { get; set; }
    public decimal DepositPercentage { get; set; }
}
