using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

using Restaurant.Core.Models.Account;

namespace Realtima_Chat_project.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        [Required]
        public string CreatorId { get; set; }

        [ForeignKey(nameof(CreatorId))]
        public ApplicationUser Creator { get; set; }

        [Required]
        [MaxLength(20)]
        public string JoinCode { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<GroupMember> Members { get; set; }
        public virtual ICollection<GroupMessage> Messages { get; set; }
    }
}
