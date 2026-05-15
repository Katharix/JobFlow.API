using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class EmployeeRoleAssignmentConfiguration : IEntityTypeConfiguration<EmployeeRoleAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeRoleAssignment> builder)
    {
        builder.ToTable("EmployeeRoleAssignments");

        builder.HasKey(era => new { era.EmployeeId, era.EmployeeRoleId });

        builder.HasOne(era => era.Employee)
            .WithMany(e => e.RoleAssignments)
            .HasForeignKey(era => era.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(era => era.Role)
            .WithMany(r => r.EmployeeAssignments)
            .HasForeignKey(era => era.EmployeeRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(era => era.EmployeeRoleId);
    }
}
