using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class HelpArticleConfiguration : IEntityTypeConfiguration<HelpArticle>
{
    public void Configure(EntityTypeBuilder<HelpArticle> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(500);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.ArticleType).IsRequired();
        builder.Property(x => x.Category).IsRequired();
        builder.Property(x => x.Tags).HasMaxLength(500);
        builder.Property(x => x.PublishedBy).HasMaxLength(128);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.IsPublished);
        builder.HasIndex(x => x.ArticleType);
        builder.HasIndex(x => x.Category);
    }
}
