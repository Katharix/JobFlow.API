namespace JobFlow.API.Models
{
    public class BrandingDto
    {
        public Guid OrganizationId { get; set; }

        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? Tagline { get; set; }
        public string? FooterNote { get; set; }
    }
}
