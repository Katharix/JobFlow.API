using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class AssignmentOrderConfiguration : IEntityTypeConfiguration<AssignmentOrder>
{
    public void Configure(EntityTypeBuilder<AssignmentOrder> builder)
    {
        builder.ToTable("AssignmentOrder");

        builder.HasKey(x => new { x.AssignmentId, x.OrderId });

        builder.HasOne(x => x.Assignment)
            .WithMany(a => a.AssignmentOrders)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Order)
            .WithMany(o => o.AssignmentOrders)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => x.Assignment.IsActive && x.Order.IsActive);

        builder.HasIndex(x => x.AssignmentId);
        builder.HasIndex(x => x.OrderId);
    }
}