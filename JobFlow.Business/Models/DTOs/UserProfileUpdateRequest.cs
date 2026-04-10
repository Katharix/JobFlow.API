namespace JobFlow.Business.Models.DTOs;

public class UserProfileUpdateRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PreferredLanguage { get; set; }
}
