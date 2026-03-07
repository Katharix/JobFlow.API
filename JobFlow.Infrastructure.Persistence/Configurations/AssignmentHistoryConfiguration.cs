using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobFlow.Infrastructure.Persistence.Configurations
{
    public class AssignmentHistoryConfiguration : IEntityTypeConfiguration<AssignmentHistory>
    {
        public void Configure(EntityTypeBuilder<AssignmentHistory> builder)
        {
            builder.ToTable("AssignmentHistory");

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Assignment)
                .WithMany()
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.AssignmentId)
                .HasDatabaseName("IX_AssignmentHistory_AssignmentId");
        }
    }
}
