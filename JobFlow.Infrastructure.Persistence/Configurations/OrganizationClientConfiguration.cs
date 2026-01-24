using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class OrganizationClientConfiguration : IEntityTypeConfiguration<OrganizationClient>
{
    public void Configure(EntityTypeBuilder<OrganizationClient> builder)
    {
        builder.ToTable("OrganizationClient");
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}