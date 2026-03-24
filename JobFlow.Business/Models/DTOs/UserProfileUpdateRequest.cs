namespace JobFlow.Business.Models.DTOs;

public class UserProfileUpdateRequest
{
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PreferredLanguage { get; set; }
}
