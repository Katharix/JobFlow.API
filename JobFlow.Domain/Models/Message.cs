

namespace JobFlow.Domain.Models
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ConversationId { get; set; }

        public Guid SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }

        public string? AttachmentUrl { get; set; } // <-- For file uploads

        public Conversation Conversation { get; set; } = null!;
        public User Sender { get; set; } = null!;
    }
}
