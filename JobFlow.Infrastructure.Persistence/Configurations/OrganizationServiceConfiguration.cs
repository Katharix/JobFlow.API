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
    public class OrganizationServiceConfiguration : IEntityTypeConfiguration<OrganizationService>
    {
        public void Configure(EntityTypeBuilder<OrganizationService> builder)
        {
            builder.ToTable("OrganizationService");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        }
    }
}
