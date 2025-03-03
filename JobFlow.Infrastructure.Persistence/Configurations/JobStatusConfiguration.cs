using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.Persistence.Configurations
{
    internal class JobStatusConfiguration : IEntityTypeConfiguration<JobStatus>
    {
        public void Configure(EntityTypeBuilder<JobStatus> builder)
        {
            builder.ToTable("JobStatus");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");

            builder.HasData(PopulateJobStatusData());
        }

        private JobStatus[] PopulateJobStatusData()
        {
            return
                [
                new JobStatus{  Id = Guid.Parse("878f4fa0-e7c4-4440-bf7e-e1bdf068b551"), Status = "Completed"},
                new JobStatus{  Id = Guid.Parse("0e58e058-c9b8-4acf-b8ca-cb8d2eb857f4"),Status = "Canceled"},
                new JobStatus{  Id = Guid.Parse("5788453f-4c21-4c9b-b50c-f987c15d0cf2"),Status = "Pending"},
                new JobStatus{  Id = Guid.Parse("001a1323-185b-4624-987c-325b23bdb5c7"),Status = "In Progress"},
                new JobStatus{  Id = Guid.Parse("0e48efd2-5783-4ea0-b091-6b4bf4377e38"),Status = "On Hold"},
                new JobStatus{  Id = Guid.Parse("be7d4999-e30f-4d4a-8f8b-580344e14dcd\r\n"),Status = "Failed"},
                new JobStatus{  Id = Guid.Parse("1ca909ea-e3e8-4364-a81f-7c5b93d9bc25\r\n"),Status = "Awaiting Approval"},
                ];
        }
    }
}
