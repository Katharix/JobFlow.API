using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.Persistence.Configurations
{
    internal class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
            builder.Ignore(e => e.EmailConfirmed);
            builder.Ignore(e => e.AccessFailedCount);
            builder.Ignore(e => e.ConcurrencyStamp);
            builder.Ignore(e => e.LockoutEnabled);
            builder.Ignore(e => e.LockoutEnd);
            builder.Ignore(e => e.NormalizedEmail);
            builder.Ignore(e => e.TwoFactorEnabled);
            builder.Ignore(e => e.NormalizedUserName);
            builder.Ignore(e => e.PasswordHash);
            builder.Ignore(e => e.SecurityStamp);
            builder.Ignore(e => e.PhoneNumberConfirmed);
        }
    }
}
