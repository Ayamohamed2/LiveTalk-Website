using System.ComponentModel.DataAnnotations.Schema;
using Realtima_Chat_project.Models;
using Restaurant.Core.Models.Account;

namespace Restaurant.Core.Entity.Chat
{
    public class Message
    {

        public int Id { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }

        public MessageType Type { get; set; }

        public string? TextContent { get; set; }
        public string? MediaUrl { get; set; }
        public int? MediaDuration { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public int? ReplyToMessageId { get; set; }

        [ForeignKey(nameof(ReplyToMessageId))]
        public Message? ReplyToMessage { get; set; }

        [ForeignKey(nameof(SenderId))]
        public ApplicationUser Sender { get; set; }


        [ForeignKey(nameof(ReceiverId))]
        public ApplicationUser Receiver { get; set; }
    }
}
