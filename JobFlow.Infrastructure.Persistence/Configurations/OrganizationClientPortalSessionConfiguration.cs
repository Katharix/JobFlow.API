using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class OrganizationClientPortalSessionConfiguration : IEntityTypeConfiguration<OrganizationClientPortalSession>
{
    public void Configure(EntityTypeBuilder<OrganizationClientPortalSession> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId).IsRequired();
        builder.Property(x => x.OrganizationClientId).IsRequired();
        builder.Property(x => x.EmailAddress).HasMaxLength(320).IsRequired();
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.TokenHash).IsUnique();

        builder.HasOne(x => x.OrganizationClient)
            .WithMany()
            .HasForeignKey(x => x.OrganizationClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
