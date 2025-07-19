namespace JobFlow.API.Models
{
    public class OnboardingDto
    {
        public Guid OrganizationId { get; set; }
        public bool OnboardingComplete { get; set; }
        public decimal DefaultTaxRate { get; set; } = 0.00m;
        public bool EnableTax { get; set; } = false;
        public BrandingDto? Branding { get; set; }
        public CustomerPaymentProfileDto? PaymentProfile { get; set; }
    }
}
