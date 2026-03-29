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
                .WithMany(a => a.AssignmentAssignees)
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(x => x.Assignment.IsActive && x.Employee.IsActive);

            builder.HasIndex(x => x.EmployeeId)
                .HasDatabaseName("IX_AssignmentAssignee_EmployeeId");
        }
    }
}
