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
    internal class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.ToTable("Organization");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");

            builder.HasOne(e => e.OrganizationType)
             .WithMany()
             .HasForeignKey(e => e.OrganizationTypeId)
             .OnDelete(DeleteBehavior.Restrict);

            builder.HasData(PopulateOrganizationData());
        }

        private Organization[] PopulateOrganizationData()
        {
            return
                [
                    new Organization
                    {
                        Id = Guid.Parse("d464b178-a52d-440b-a064-42246f7e0756"),
                        OrganizationTypeId = Guid.Parse("6ac2cabc-bbe3-4bc1-9879-5455de042cf4"),
                        OrganizationName = "Katharix",         
                        HasFreeAccount = true,
                        EmailAddress = "jerry.daniel.phillips@gmail.com",
                    },
                    new Organization
                    {
                        Id = Guid.Parse("b3b20208-07ae-40a2-971e-adf3bb93fc8c"),
                        OrganizationTypeId = Guid.Parse("1921d982-22f8-4ed5-b4e3-fca82c5767eb"),
                        OrganizationName = "Browns Cleaning Services",
                        Address1 = "116 Terrill St",
                        City = "Beckley",
                        State = "WV",
                        ZipCode = "25801",
                        HasFreeAccount = true,
                        PhoneNumber = "304-731-1952",
                        EmailAddress = "vonbrown230@gmail.com",
                    }
                ];
        }
    }
}
