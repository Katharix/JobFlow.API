
namespace JobFlow.Domain.Models
{
    public class ConversationParticipant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ConversationId { get; set; }
        public Guid UserId { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public User User { get; set; } = null!;
    }

}
