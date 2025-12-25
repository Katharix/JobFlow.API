using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class OrganizationServiceConfiguration : IEntityTypeConfiguration<OrganizationService>
{
    public void Configure(EntityTypeBuilder<OrganizationService> builder)
    {
        builder.ToTable("OrganizationService");
        builder.HasKey(e => e.Id);
    }
}