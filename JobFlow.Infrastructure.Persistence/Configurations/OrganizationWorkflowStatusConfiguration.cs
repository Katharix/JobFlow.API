using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class OrganizationWorkflowStatusConfiguration : IEntityTypeConfiguration<OrganizationWorkflowStatus>
{
    public void Configure(EntityTypeBuilder<OrganizationWorkflowStatus> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.Category, x.StatusKey }).IsUnique();

        builder.Property(x => x.Category).HasMaxLength(60).IsRequired();
        builder.Property(x => x.StatusKey).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(120).IsRequired();
    }
}
