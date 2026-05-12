using Microsoft.AspNetCore.Http;
using Realtima_Chat_project.Models;

namespace Realtima_Chat_project.DTO
{
    public class SendGroupMessageDTO
    {
        public int GroupId { get; set; }
        public string? Text { get; set; }
        public IFormFile? File { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public int? MediaDuration { get; set; }
    }
}
