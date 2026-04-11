using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
{
    public void Configure(EntityTypeBuilder<ShortLink> builder)
    {
        builder.ToTable("ShortLink", "notifications");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(16).IsRequired();
        builder.Property(e => e.TargetUrl).HasMaxLength(2048).IsRequired();

        builder.HasIndex(e => e.Code).IsUnique().HasFilter("[IsActive] = 1");
        builder.HasIndex(e => e.CreatedAt);
    }
}
