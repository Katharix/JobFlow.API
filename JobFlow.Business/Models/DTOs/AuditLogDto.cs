namespace JobFlow.Business.Models.DTOs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string? IpAddress { get; set; }
    public string? DetailsJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
