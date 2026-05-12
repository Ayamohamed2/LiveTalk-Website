using NEEFRA.Core.DTO.Service;
using Realtima_Chat_project.DTO;
using Realtima_Chat_project.Models;
using Restaurant.Core.DTO.Chat;

namespace NEEFRA.Core
{
    public interface IChatService
    {
        Task<ServiceResult<object>> SendMessageAsync(string senderId, MessageDTO dto, string baseUrl);
        Task<ServiceResult<object>> ReplyToMessageAsync(string senderId, ReplyToMessageDTOFor_OneToOne dto, string baseUrl);
        Task<ServiceResult<IEnumerable<object>>> GetUsersAsync(string currentUserId, string baseUrl);
        Task<ServiceResult<IEnumerable<object>>> GetMessagesWithUserAsync(string currentUserId, string otherUserId, string baseUrl);
        Task<ServiceResult<object>> MarkAsDeliveredAsync(string currentUserId, int messageId);
        Task<ServiceResult<object>> MarkAsReadAsync(string currentUserId, string senderId);
        Task<ServiceResult<object>> MarkSingleAsReadAsync(string currentUserId, int messageId);
        Task<ServiceResult<int>> GetUnreadCountAsync(string currentUserId, string senderId);
        Task<ServiceResult<object>> DeleteMessageForMeAsync(string currentUserId, int messageId);
        Task<ServiceResult<object>> DeleteMessageForEveryoneAsync(string currentUserId, int messageId);
        Task<ServiceResult<object>> ClearChatAsync(string currentUserId, string otherUserId);
        Task<ServiceResult<object>> BlockUserAsync(string blockerId, string targetUserId);
        Task<ServiceResult<object>> UnblockUserAsync(string blockerId, string targetUserId);
        Task<ServiceResult<IEnumerable<object>>> GetBlockedUsersAsync(string userId, string baseUrl);
        Task<ServiceResult<IEnumerable<ChatListItemDTO>>> GetMyChatListAsync(string currentUserId, string baseUrl);
    }
}
