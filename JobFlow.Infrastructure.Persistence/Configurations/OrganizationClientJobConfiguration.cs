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
    internal class OrganizationClientJobConfiguration : IEntityTypeConfiguration<OrganizationClientJob>
    {
        public void Configure(EntityTypeBuilder<OrganizationClientJob> builder)
        {
            builder.ToTable("OrganizationClientJob");
            builder.HasKey(e => new { e.JobId, e.OrganizationClientId });
            builder
                .HasOne(j => j.Job)
                .WithMany(jo => jo.OrganizationClientJobs)
                .HasForeignKey(e => e.JobId);

            builder
                .HasOne(cj => cj.OrganizationClient)
                .WithMany(jo => jo.OrganizationClientJobs)
                .HasForeignKey(e => e.OrganizationClientId);
        }
    }
}
