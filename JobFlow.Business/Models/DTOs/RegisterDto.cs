namespace JobFlow.Business.Models.DTOs;

public class RegisterDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public Guid OrganizationId { get; set; }
    public string Role { get; set; }
}