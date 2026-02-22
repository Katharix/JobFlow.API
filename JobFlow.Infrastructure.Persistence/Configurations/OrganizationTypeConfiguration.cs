using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class OrganizationTypeConfiguration : IEntityTypeConfiguration<OrganizationType>
{
    public void Configure(EntityTypeBuilder<OrganizationType> builder)
    {
        builder.ToTable("OrganizationType");
        builder.HasKey(e => e.Id);
    }

    private OrganizationType[] PopulateOrganizationTypeData()
    {
        return
        [
            new OrganizationType
                { Id = Guid.Parse("bf489aa6-db19-42df-82bc-c116bd967e7e"), TypeName = "General Contracting" },
            new OrganizationType { Id = Guid.Parse("393a5b3e-323e-4b76-aa86-0d4683ddcd49"), TypeName = "Painting" },
            new OrganizationType { Id = Guid.Parse("e01750b0-0d01-4e25-abf7-6efa23509035"), TypeName = "Plumbing" },
            new OrganizationType
                { Id = Guid.Parse("fbc6accf-0fb1-4908-b449-14f13b826f24"), TypeName = "Landscaping and Gardening" },
            new OrganizationType
                { Id = Guid.Parse("9362c957-0f41-4c20-9085-c01e449fdda2"), TypeName = "Electrical Services" },
            new OrganizationType
                { Id = Guid.Parse("8f0d3e93-425b-4a53-b4d2-4c5eb97e490f"), TypeName = "Carpentry and Woodworking" },
            new OrganizationType
                { Id = Guid.Parse("37fc17e8-0a25-4119-9a71-7d160bb9c7b4"), TypeName = "HVAC Services" },
            new OrganizationType { Id = Guid.Parse("f64f078f-ecfb-4f3e-8640-236219fcf01e"), TypeName = "Tree Removal" },
            new OrganizationType { Id = Guid.Parse("bf3b9512-8a9c-4a73-9f88-cb914c1573cd"), TypeName = "Pest Control" },
            new OrganizationType
                { Id = Guid.Parse("1921d982-22f8-4ed5-b4e3-fca82c5767eb"), TypeName = "Cleaning Services" },
            new OrganizationType { Id = Guid.Parse("408d2185-53b9-493d-8713-938114de90f5"), TypeName = "Junk Removal" },
            new OrganizationType
                { Id = Guid.Parse("33341b2d-957f-4efb-94f7-3a015ae1a718"), TypeName = "Car Detailing" },
            new OrganizationType
                { Id = Guid.Parse("30530a32-a151-436d-a050-613eac4c22d5"), TypeName = "IT & Network Installation" },
            new OrganizationType
                { Id = Guid.Parse("6ac2cabc-bbe3-4bc1-9879-5455de042cf4"), TypeName = "Master Account" },
            new OrganizationType { Id = Guid.Parse("0F32E14A-5F70-45AF-A647-04E59AD52E58"), TypeName = "Handyman" },
            new OrganizationType { Id = Guid.Parse("09786EAB-D69F-45BF-BCEC-5F368BD60BE7"), TypeName = "Flooring" },
            new OrganizationType { Id = Guid.Parse("906A2BDB-4CC6-4E49-ACC3-1BD63FB82611"), TypeName = "Other" }
        ];
    }
}