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
    internal class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Message", "messaging");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Content).IsRequired();

            builder.HasOne(m => m.Sender)
                   .WithMany()
                   .HasForeignKey(m => m.SenderId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
