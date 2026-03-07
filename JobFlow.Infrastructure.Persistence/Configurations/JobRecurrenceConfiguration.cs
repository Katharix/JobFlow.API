using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobFlow.Infrastructure.Persistence.Configurations
{

    public class JobRecurrenceConfiguration : IEntityTypeConfiguration<JobRecurrence>
    {
        public void Configure(EntityTypeBuilder<JobRecurrence> builder)
        {
            builder.ToTable("JobRecurrence");

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Job)
                .WithMany()
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.JobId, x.IsActive })
                .HasDatabaseName("IX_JobRecurrence_JobId_IsActive");
        }
    }
}
