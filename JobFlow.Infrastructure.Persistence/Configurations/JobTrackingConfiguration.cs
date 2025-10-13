using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.Persistence.Configurations
{
    using JobFlow.Domain.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class JobTrackingConfiguration : IEntityTypeConfiguration<JobTracking>
    {
        public void Configure(EntityTypeBuilder<JobTracking> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Latitude)
                .HasColumnType("decimal(9,6)");

            builder.Property(t => t.Longitude)
                .HasColumnType("decimal(9,6)");

            builder.Property(t => t.RecordedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(t => t.Job)
                .WithMany(j => j.JobTrackings)
                .HasForeignKey(t => t.JobId);

            builder.HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => t.EmployeeId);
        }
    }

}
