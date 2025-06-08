

namespace JobFlow.Domain.Models
{
    public class Conversation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Title { get; set; } // Optional – could be job title or username

        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

}
