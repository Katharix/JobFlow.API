using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class EstimateSequenceConfiguration : IEntityTypeConfiguration<EstimateSequence>
{
    public void Configure(EntityTypeBuilder<EstimateSequence> builder)
    {
        builder.ToTable("EstimateSequence");
        builder.HasKey(e => e.Id);
    }
}
