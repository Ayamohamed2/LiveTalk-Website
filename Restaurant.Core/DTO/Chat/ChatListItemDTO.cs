using Realtima_Chat_project.Models;

namespace Restaurant.Core.DTO.Chat
{
    public class ChatListItemDTO
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string? Email { get; set; }
        public string? ImageUrl { get; set; }
        public int UnreadCount { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsBlockedByMe { get; set; }
        public bool IBlockedThem { get; set; }

        public string? LastMessageText { get; set; }
        public MessageType? LastMessageType { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public DateTime? LastSeen { get; set; }

        public bool? LastMessageFromMe { get; set; }
    }
}
