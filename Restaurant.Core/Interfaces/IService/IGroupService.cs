using NEEFRA.Core.DTO.Service;
using Realtima_Chat_project.DTO;
using Restaurant.Core.DTO.Group;

namespace NEEFRA.Core
{
    public interface IGroupService
    {
        // ─────────────────────────────────────────────
        // Group Management
        // ─────────────────────────────────────────────
        Task<ServiceResult<object>> CreateGroupAsync(string userId, CreateGroupDTO dto, string baseUrl);
        Task<ServiceResult<object>> JoinGroupAsync(string userId, JoinGroupDTO dto, string baseUrl);
        Task<ServiceResult<object>> AddMembersAsync(string userId, AddMembersDTO dto, string baseUrl);
        Task<ServiceResult<object>> RemoveMemberAsync(string userId, RemoveMemberDTO dto);
        Task<ServiceResult<object>> LeaveGroupAsync(string userId, int groupId);
        Task<ServiceResult<object>> UpdateGroupAsync(string userId, int groupId, UpdateGroupDTO dto, string baseUrl);
        Task<ServiceResult<object>> GetGroupByIdAsync(string userId, int groupId, string baseUrl);
        Task<ServiceResult<List<object>>> GetMyGroupsAsync(string userId, string baseUrl);
        Task<ServiceResult<IEnumerable<object>>> GetGroupMembersAsync(string userId, int groupId, string baseUrl);
        Task<ServiceResult<IEnumerable<object>>> GetAvailableUsersAsync(string userId, int groupId, string baseUrl);

        // ─────────────────────────────────────────────
        // Messaging
        // ─────────────────────────────────────────────
        Task<ServiceResult<object>> SendMessageAsync(string senderId, SendGroupMessageDTO dto, string baseUrl);
        Task<ServiceResult<object>> ReplyToMessageAsync(string senderId, ReplyToMessageDTO dto, string baseUrl);
        Task<ServiceResult<List<object>>> GetGroupMessagesAsync(string userId, int groupId, string baseUrl);
        Task<ServiceResult<object>> DeleteMessageForMeAsync(string userId, int messageId);
        Task<ServiceResult<object>> DeleteMessageForEveryoneAsync(string userId, int messageId);
        Task<ServiceResult<object>> ClearChatAsync(string userId, int groupId);

        // ─────────────────────────────────────────────
        // Read & Unread
        // ─────────────────────────────────────────────
        Task<ServiceResult<object>> MarkAsReadAsync(string userId, int messageId);
        Task<ServiceResult<IEnumerable<object>>> WhoReadAsync(string userId, int messageId, string baseUrl);
        Task<ServiceResult<int>> GetUnreadCountAsync(string userId, int groupId);
    }
}
