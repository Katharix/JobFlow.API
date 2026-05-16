using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class EmployeeInviteRoleAssignmentConfiguration : IEntityTypeConfiguration<EmployeeInviteRoleAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeInviteRoleAssignment> builder)
    {
        builder.ToTable("EmployeeInviteRoleAssignments");

        builder.HasKey(era => new { era.EmployeeInviteId, era.EmployeeRoleId });

        builder.HasOne(era => era.EmployeeInvite)
            .WithMany(i => i.RoleAssignments)
            .HasForeignKey(era => era.EmployeeInviteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(era => era.Role)
            .WithMany(r => r.InviteAssignments)
            .HasForeignKey(era => era.EmployeeRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(era => era.EmployeeRoleId);
    }
}
