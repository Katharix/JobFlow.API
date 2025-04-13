using JobFlow.Domain.Models;
using JobFlow.Domain.Models.JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations
{
    internal class SubscriptionRecordConfiguration : IEntityTypeConfiguration<SubscriptionRecord>
    {
        public void Configure(EntityTypeBuilder<SubscriptionRecord> builder)
        {
            builder.ToTable("SubscriptionRecord", "payment");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.ProviderSubscriptionId)
                   .IsRequired()
                   .HasMaxLength(128);

            builder.Property(e => e.ProviderPriceId)
                   .IsRequired()
                   .HasMaxLength(128);

            builder.Property(e => e.Status)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(e => e.StartDate)
                   .IsRequired();

            builder.HasOne(e => e.PaymentProfile)
                   .WithMany()
                   .HasForeignKey(e => e.PaymentProfileId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
