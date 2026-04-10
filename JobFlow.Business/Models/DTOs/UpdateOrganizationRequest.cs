namespace JobFlow.Business.Models.DTOs;

public class UpdateOrganizationRequest
{
    public string? OrganizationName { get; set; }
    public Guid? OrganizationTypeId { get; set; }
    public string? ContactFirstName { get; set; }
    public string? ContactLastName { get; set; }
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
}
