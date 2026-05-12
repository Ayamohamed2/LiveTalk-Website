using Microsoft.AspNetCore.Http;
using Realtima_Chat_project.Models;
using static Restaurant.Core.Entity.Chat.Message;

namespace Restaurant.Core.DTO.Chat
{
    public class MessageDTO
    {
        public string ReceiverId { get; set; }
        public string? Text { get; set; }
        public IFormFile? File { get; set; }
        public int? MediaDuration { get; set; }
        public MessageType Type { get; set; }
    }
}
