using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Restaurant.Core.Models.Account;

namespace Realtima_Chat_project.Models
{
    public class GroupMember
    {
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }

        [ForeignKey(nameof(GroupId))]
        public Group Group { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.Now;

        public bool IsAdmin { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public int? LastReadMessageId { get; set; }

    }
}
