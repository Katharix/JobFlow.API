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
    public class OrganizationOnboardingStepConfiguration : IEntityTypeConfiguration<OrganizationOnboardingStep>
    {
        public void Configure(EntityTypeBuilder<OrganizationOnboardingStep> builder)
        {
            builder.ToTable("OrganizationOnboardingSteps");
            builder.Property(x => x.StepName)
                   .IsRequired()
                   .HasMaxLength(100);
            builder.HasIndex(x => new { x.OrganizationId, x.StepName }).IsUnique();

            builder.HasOne(x => x.Organization)
                   .WithMany(o => o.OnboardingSteps)
                   .HasForeignKey(x => x.OrganizationId);
        }
    }
}
