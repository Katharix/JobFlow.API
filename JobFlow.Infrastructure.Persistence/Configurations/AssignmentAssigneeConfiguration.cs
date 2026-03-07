using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobFlow.Infrastructure.Persistence.Configurations
{
    public class AssignmentAssigneeConfiguration : IEntityTypeConfiguration<AssignmentAssignee>
    {
        public void Configure(EntityTypeBuilder<AssignmentAssignee> builder)
        {
            builder.ToTable("AssignmentAssignee");

            builder.HasKey(x => new { x.AssignmentId, x.EmployeeId });

            builder.HasOne(x => x.Assignment)
                .WithMany()
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.EmployeeId)
                .HasDatabaseName("IX_AssignmentAssignee_EmployeeId");
        }
    }
}
