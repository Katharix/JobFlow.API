using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Job");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .HasMaxLength(200);

        builder.Property(e => e.Comments)
            .HasMaxLength(2000);

        builder.Property(j => j.LifecycleStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(j => j.InvoicingWorkflow)
            .HasConversion<int>();

        // ✅ Relationship with OrganizationClient
        builder.HasOne(j => j.OrganizationClient)
            .WithMany(c => c.Jobs) // assuming you added ICollection<Job> Jobs to OrganizationClient
            .HasForeignKey(j => j.OrganizationClientId)
            .OnDelete(DeleteBehavior.Restrict); // 👈 avoids multiple cascade paths
    }
}