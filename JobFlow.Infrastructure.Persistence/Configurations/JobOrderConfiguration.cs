using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class JobOrderConfiguration : IEntityTypeConfiguration<JobOrder>
{
    public void Configure(EntityTypeBuilder<JobOrder> builder)
    {
        builder.ToTable("JobOrder");
        builder.HasKey(jo => new { jo.JobId, jo.OrderId });

        builder.HasOne(jo => jo.Job)
            .WithMany(j => j.JobOrders)
            .HasForeignKey(jo => jo.JobId);

        builder.HasOne(jo => jo.Order)
            .WithMany(o => o.JobOrders)
            .HasForeignKey(jo => jo.OrderId);

        builder.Property(jo => jo.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}