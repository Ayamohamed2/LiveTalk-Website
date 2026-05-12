using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

using Restaurant.Core.Models.Account;

namespace Realtima_Chat_project.Models
{
    public class GroupMessageRead
    {
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }

        [ForeignKey(nameof(MessageId))]
        public GroupMessage Message { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }

        public DateTime ReadAt { get; set; } = DateTime.Now;
    }
}
