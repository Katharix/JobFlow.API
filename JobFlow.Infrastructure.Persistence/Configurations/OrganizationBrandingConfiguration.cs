using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class OrganizationBrandingConfiguration : IEntityTypeConfiguration<OrganizationBranding>
{
    public void Configure(EntityTypeBuilder<OrganizationBranding> builder)
    {
        builder.ToTable("OrganizationBranding");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.OrganizationId)
            .IsUnique();

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
