namespace JobFlow.Business.Models.DTOs;

public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string Role { get; set; } = string.Empty;
}