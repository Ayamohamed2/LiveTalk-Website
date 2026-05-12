using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Restaurant.Core.Models.Account;

namespace Realtima_Chat_project.Models
{
    public class GroupMessage
    {
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }

        [ForeignKey(nameof(GroupId))]
        public Group Group { get; set; }

        [Required]
        public string SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public ApplicationUser Sender { get; set; }

        public MessageType Type { get; set; } = MessageType.Text;

        [MaxLength(5000)]
        public string? TextContent { get; set; }

        [MaxLength(500)]
        public string? MediaUrl { get; set; }

        public int? MediaDuration { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsForwarded { get; set; } = false;

        public int? ForwardedFromMessageId { get; set; }
        public int? ReplyToMessageId { get; set; }

        [ForeignKey(nameof(ReplyToMessageId))]
        public GroupMessage? ReplyToMessage { get; set; }
        public virtual ICollection<GroupMessageRead> ReadBy { get; set; }
    }

    public enum MessageType
    {
        Text = 0,
        Image = 1,
        Video = 2,
        Voice = 3,
       
    }
}

