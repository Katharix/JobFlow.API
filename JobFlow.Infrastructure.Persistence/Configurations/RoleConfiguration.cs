using JobFlow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.Persistence.Configurations
{
    internal class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
    {
        public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
        {
            builder.ToTable("Roles");
            builder.Property(u => u.Id).HasDefaultValueSql("NEWID()");
            builder.HasData(PopulateRoleData());
        }

        private IdentityRole<Guid>[] PopulateRoleData()
        {
            return 
                [
                    new IdentityRole<Guid>  { Id = Guid.Parse("e88fbbe6-8bdf-4aca-b941-912785a94f0b"),Name = UserRoles.OrganizationAdmin, NormalizedName = UserRoles.OrganizationAdmin.ToUpper() },
                    new IdentityRole<Guid>  { Id = Guid.Parse("079e4277-0eb2-4222-82e4-5a751ede48f6"),Name = UserRoles.OrganizationEmployee, NormalizedName = UserRoles.OrganizationEmployee.ToUpper() },
                    new IdentityRole<Guid>  { Id = Guid.Parse("3da14c58-562a-437a-a2a6-47706b40eb70"),Name = UserRoles.OrganizationClient, NormalizedName = UserRoles.OrganizationClient.ToUpper() },
                    new IdentityRole<Guid>  { Id = Guid.Parse("5bc0d325-a915-4e17-8184-428ee533cf89"),Name = UserRoles.KatharixAdmin, NormalizedName = UserRoles.KatharixAdmin.ToUpper() },
                    new IdentityRole<Guid>  { Id = Guid.Parse("92193eb2-dba0-433c-814e-9fca95bde016"),Name = UserRoles.KatharixEmployee, NormalizedName = UserRoles.KatharixEmployee.ToUpper() },
                    new IdentityRole<Guid>  { Id = Guid.Parse("dfe36ebc-bfb5-4583-b68e-59be8ba60fa9"),Name = UserRoles.SuperAdmin, NormalizedName = UserRoles.SuperAdmin.ToUpper() }
                ];
        }
    }
}
