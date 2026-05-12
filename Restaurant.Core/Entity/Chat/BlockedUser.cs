using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Restaurant.Core.Models.Account;

namespace Restaurant.Core.Entity.Chat
{
    public class BlockedUser
    {
        public int Id { get; set; }

        [Required]
        public string BlockerId { get; set; }

        [ForeignKey(nameof(BlockerId))]
        public ApplicationUser? Blocker { get; set; }

        [Required]
        public string BlockedUserId { get; set; }

        [ForeignKey(nameof(BlockedUserId))]
        public ApplicationUser? BlockedUserEntity { get; set; }

        public DateTime BlockedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
