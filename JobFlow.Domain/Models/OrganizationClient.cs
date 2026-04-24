namespace JobFlow.Domain.Models;

public class OrganizationClient : Entity
{
    public Guid OrganizationId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? ZipCode { get; set; }
    public bool SmsConsentGiven { get; set; } = false;

    public virtual Organization Organization { get; set; } = null!;

    // ✅ Replace the old join collection with a direct Jobs collection
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

    public ICollection<CustomerPaymentProfile> PaymentProfiles { get; set; } = new List<CustomerPaymentProfile>();

    public string ClientFullName()
    {
        return $"{FirstName} {LastName}";
    }
}