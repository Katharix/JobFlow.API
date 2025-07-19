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
    internal class EmployeeRoleConfiguration : IEntityTypeConfiguration<EmployeeRole>
    {
        public void Configure(EntityTypeBuilder<EmployeeRole> builder)
        {
            builder.ToTable("EmployeeRoles");
            builder.HasKey(er => er.Id);
            builder.HasOne(er => er.Organization)
                   .WithMany(o => o.EmployeeRoles)
                   .HasForeignKey(er => er.OrganizationId)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.Property(er => er.Name)
                   .IsRequired()
                   .HasMaxLength(100);
        }
    }
}
