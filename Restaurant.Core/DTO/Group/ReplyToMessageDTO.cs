using Microsoft.AspNetCore.Http;
using Realtima_Chat_project.Models;

namespace Restaurant.Core.DTO.Group
{
    public class ReplyToMessageDTO
    {

        public int GroupId { get; set; }
        public int ReplyToMessageId { get; set; }
        public string? Text { get; set; }
        public IFormFile? File { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public int? MediaDuration { get; set; }
    }
}
