namespace JobFlow.API.Models
{
    public class OrganizationClientDto
    {
        public Guid? Id { get; set; }
        public Guid? OrganizationId { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }

        public string? PhoneNumber { get; set; }
        public string? EmailAddress { get; set; }

        public string? FullName => $"{FirstName} {LastName}".Trim();
       
        public OrganizationDto Organization { get; set; }
    }

    public class OrganizationDto
    {
        public Guid? Id { get; set; }
        public string? OrganizationName { get; set; }
        public string? Email { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public decimal DefaultTaxRate { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? OnBoardingComplete { get; set; }
        public bool CanAcceptPayments { get; set; }
    }
}
