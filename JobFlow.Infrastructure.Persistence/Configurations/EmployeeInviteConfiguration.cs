using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JobFlow.Domain.Enums;

namespace JobFlow.Infrastructure.Persistence.Configurations
{
    public class EmployeeInviteConfiguration : IEntityTypeConfiguration<EmployeeInvite>
    {
        public void Configure(EntityTypeBuilder<EmployeeInvite> builder)
        {
            builder.ToTable("EmployeeInvites");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Email)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(e => e.LastName)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.InviteToken)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(e => e.ExpiresAt)
                .IsRequired();

            builder.Property(e => e.Status)
                .HasDefaultValue(EmployeeInviteStatus.Pending);
            
            builder.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasIndex(x => x.InviteToken)
                .IsUnique();
            
            builder.HasIndex(x => new { x.OrganizationId, x.Email })
                .IsUnique()
                .HasFilter("[Status] = 1"); // Pending
            
            builder.HasIndex(x => x.OrganizationId);
            
            builder.HasIndex(x => new { x.Status, x.ExpiresAt });
            
            builder.HasIndex(x => x.ShortCode)
                .IsUnique()
                .HasFilter("[ShortCode] IS NOT NULL");
        }
    }
}
