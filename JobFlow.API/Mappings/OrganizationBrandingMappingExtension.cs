using JobFlow.API.Models;
using JobFlow.Domain.Models;

namespace JobFlow.API.Mappings;

public static class OrganizationBrandingMapper
{
    public static BrandingDto ToDto(OrganizationBranding entity)
    {
        return new BrandingDto
        {
            OrganizationId = entity.OrganizationId,
            LogoUrl = entity.LogoUrl,
            PrimaryColor = entity.PrimaryColor,
            SecondaryColor = entity.SecondaryColor,
            Tagline = entity.Tagline,
            FooterNote = entity.FooterNote
        };
    }

    public static OrganizationBranding ToEntity(BrandingDto dto)
    {
        return new OrganizationBranding
        {
            OrganizationId = dto.OrganizationId,
            LogoUrl = dto.LogoUrl,
            PrimaryColor = dto.PrimaryColor,
            SecondaryColor = dto.SecondaryColor,
            Tagline = dto.Tagline,
            FooterNote = dto.FooterNote
        };
    }
}