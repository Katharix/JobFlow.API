using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure;

public abstract class EntityTypeConfiguration<TEntity> where TEntity : class
{
    public abstract void Map(EntityTypeBuilder<TEntity> builder);
}

public static class ModelBuilderExtensions
{
    public static void AddConfiguration<TEntity>(ModelBuilder modelBuilder,
        EntityTypeConfiguration<TEntity> configuration)
        where TEntity : class
    {
        configuration.Map(modelBuilder.Entity<TEntity>());
    }
}