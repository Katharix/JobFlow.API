using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class SupportChatMessageConfiguration : IEntityTypeConfiguration<SupportChatMessage>
{
    public void Configure(EntityTypeBuilder<SupportChatMessage> builder)
    {
        builder.ToTable("SupportChatMessage", "support");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SenderName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.SenderRole).IsRequired();
        builder.Property(x => x.FileUrl).HasMaxLength(2048);
        builder.Property(x => x.FileName).HasMaxLength(255);
        builder.Property(x => x.SentAt).IsRequired();

        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.SentAt);
    }
}
