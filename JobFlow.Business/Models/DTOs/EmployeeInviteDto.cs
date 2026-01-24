namespace JobFlow.Business.Models.DTOs;

public class EmployeeInviteDto
{
    public Guid? Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RoleId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public DateTime ExpiresAt { get; set; }
    public bool IsAccepted { get; set; }
    public bool IsRevoked { get; set; }

    // Optional: only expose token if needed for acceptance
    public string? InviteToken { get; set; }

    // Optional: lightweight display info
    public string? RoleName { get; set; }
    public string? OrganizationName { get; set; }
}