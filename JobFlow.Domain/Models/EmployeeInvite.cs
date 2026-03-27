using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class EmployeeInvite : Entity
{
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid RoleId { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid InviteToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public EmployeeInviteStatus Status { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string ShortCode { get; set; } = string.Empty;
    public DateTime? AccessedAt { get; set; }
    public int AccessCount { get; set; }
    public string? AccessIpAddress { get; set; }
    public Organization Organization { get; set; } = null!;
    public EmployeeRole Role { get; set; } = null!;
}