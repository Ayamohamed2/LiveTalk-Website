using Realtima_Chat_project.Models;
using Restaurant.Core.DTO.Chat;

namespace Realtima_Chat_project.DTO
{
    public class GroupMessageDTO
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string? SenderImage { get; set; }
        public MessageType Type { get; set; }
        public string? TextContent { get; set; }
        public string? MediaUrl { get; set; }
        public int? MediaDuration { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsForwarded { get; set; }
        public int TotalMembers { get; set; }          
        public int ReadByCount { get; set; }           
        public bool ReadByMe { get; set; }               
        public bool ReadByAll { get; set; }
        public List<MessageReadInfoDTO>? ReadBy { get; set; }

        public int? ReplyToMessageId { get; set; }
        public string? ReplyToText { get; set; }
        public string? ReplyToSenderName { get; set; }
        public MessageType? ReplyToType { get; set; }
    }
}
