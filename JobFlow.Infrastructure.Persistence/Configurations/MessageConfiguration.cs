using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Message", "messaging");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.SenderId).IsRequired(false);

        builder.Property(e => e.ExternalSenderName).HasMaxLength(200);
        builder.Property(e => e.ExternalSenderType).HasMaxLength(50);
        builder.Property(e => e.ExternalSenderPhone).HasMaxLength(32);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}