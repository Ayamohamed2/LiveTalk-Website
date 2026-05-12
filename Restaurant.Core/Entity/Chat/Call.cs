using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Restaurant.Core.Models.Account;

namespace Restaurant.Core.Entity.Chat
{
    public class Call
    {
        public int Id { get; set; }

        [Required]

        public string CallerId { get; set; }

        [Required]
        public string ReceiverId { get; set; }

        [Required]
        public CallType CallType { get; set; }

        [Required]
        public CallStatus CallStatus { get; set; }

        [Required]
        public DateTime StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        public int Duration { get; set; }

        [ForeignKey("CallerId")]
        public virtual ApplicationUser Caller { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual ApplicationUser Receiver { get; set; }
    }

    public enum CallType
    {
        Voice = 0,
        Video = 1
    }

    public enum CallStatus
    {
        Ringing = 0,
        Active = 1,
        Ended = 2,
        Rejected = 3,
        Missed = 4,
        Busy = 5
    }
}

