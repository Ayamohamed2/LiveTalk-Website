using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

using Restaurant.Core.Models.Account;

namespace Realtima_Chat_project.Models
{
    public class GroupMessageDeleted
    {
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }

        [ForeignKey(nameof(MessageId))]
        public GroupMessage? Message { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public DateTime DeletedAt { get; set; }

        public bool DeletedForEveryone { get; set; }
    }
}
