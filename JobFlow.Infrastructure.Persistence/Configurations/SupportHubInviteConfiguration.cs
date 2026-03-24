using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class SupportHubInviteConfiguration : IEntityTypeConfiguration<SupportHubInvite>
{
    public void Configure(EntityTypeBuilder<SupportHubInvite> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(12).IsRequired();
        builder.Property(x => x.Role).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.RedeemedByUid).HasMaxLength(128);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
