

namespace JobFlow.Domain.Models
{
    public class Conversation : Entity
    {
        public string? Title { get; set; } // Optional – could be job title or username
        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

}
