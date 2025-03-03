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
    internal class OrganizationClientConfiguration : IEntityTypeConfiguration<OrganizationClient>
    {
        public void Configure(EntityTypeBuilder<OrganizationClient> builder)
        {
            builder.ToTable("OrganizationClient");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
            builder.HasOne(e => e.Organization)
             .WithMany()
             .HasForeignKey(e => e.OrganizationId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
