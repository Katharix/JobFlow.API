using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("ConversationParticipant", "messaging");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.ConversationId, e.UserId }).IsUnique();
        builder.HasQueryFilter(e => e.Conversation.IsActive && e.User.IsActive);
    }
}