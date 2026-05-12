using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NEEFRA.Core;
using NEEFRA.Core.DTO.Service;
using Realtima_Chat_project.DTO;
using Realtima_Chat_project.Models;
using Restaurant.Core.Models.Account;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;
using Realtima_Chat_project.Hubs;
using Restaurant.Core.Entity.Chat;
using Microsoft.AspNetCore.Http;
using Restaurant.Core.DTO.Chat;
using SignalIR_practice.Hubs;
namespace NEEFRA.Core.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unit;
        private readonly IHubContext<ChatHub> _hub;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IUnitOfWork unit,
            IHubContext<ChatHub> hub,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            ILogger<ChatService> logger)
        {
            _unit = unit;
            _hub = hub;
            _userManager = userManager;
            _env = env;
            _logger = logger;
        }



        private bool IsBlocked(string userId1, string userId2)
        {
            return _unit.BlockedUsers.GetALL(b =>
                (b.BlockerId == userId1 && b.BlockedUserId == userId2 && b.IsActive) ||
                (b.BlockerId == userId2 && b.BlockedUserId == userId1 && b.IsActive)
            ).Any();
        }

        private Message BuildMessage(
            string senderId,
            string receiverId,
            MessageType type,
            string? text,
            IFormFile? file,
            int? mediaDuration = null,
            int? replyToId = null)
        {
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Type = type,
                CreatedAt = DateTime.Now,
                ReplyToMessageId = replyToId
            };

            if (type == MessageType.Text)
            {
                message.TextContent = text;
            }
            else if (file != null)
            {
                message.MediaUrl = _unit.Message.GetImageURL(file, _env, type);
                if (mediaDuration.HasValue)
                    message.MediaDuration = mediaDuration.Value;
            }

            return message;
        }

        private static string? ResolveUrl(string? relativeUrl, string baseUrl)
            => string.IsNullOrEmpty(relativeUrl) ? null : $"{baseUrl}/{relativeUrl}";



        public async Task<ServiceResult<object>> SendMessageAsync(string senderId, MessageDTO dto, string baseUrl)
        {
            _logger.LogInformation("SendMessage: sender={SenderId} → receiver={ReceiverId}", senderId, dto.ReceiverId);

            if (IsBlocked(senderId, dto.ReceiverId))
            {
                _logger.LogWarning("SendMessage blocked: sender={SenderId}, receiver={ReceiverId}", senderId, dto.ReceiverId);
                return new() { IsSuccess = false, Message = "Cannot send message to blocked user", ErrorType = "BadRequest" };
            }

            var message = BuildMessage(senderId, dto.ReceiverId, dto.Type, dto.Text, dto.File, dto.MediaDuration);

            _unit.Message.Create(message);
            _unit.save();

            message.MediaUrl = ResolveUrl(message.MediaUrl, baseUrl);

            await _hub.Clients.User(dto.ReceiverId).SendAsync("ReceiveMessage", message);
            await _hub.Clients.User(senderId).SendAsync("ReceiveMessage", message);

            _logger.LogInformation("SendMessage success: messageId={MessageId}", message.Id);
            return new() { IsSuccess = true, Data = (object)message };
        }

        // ─────────────────────────────────────────────
        // Reply To Message
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> ReplyToMessageAsync(string senderId, ReplyToMessageDTOFor_OneToOne dto, string baseUrl)
        {
            _logger.LogInformation("ReplyToMessage: sender={SenderId}, replyTo={ReplyToMessageId}", senderId, dto.ReplyToMessageId);

            if (IsBlocked(senderId, dto.ReceiverId))
            {
                _logger.LogWarning("ReplyToMessage blocked: sender={SenderId}", senderId);
                return new() { IsSuccess = false, Message = "Cannot send message to blocked user", ErrorType = "BadRequest" };
            }

            var originalMessage = _unit.Message.GetByFilter(m => m.Id == dto.ReplyToMessageId);
            if (originalMessage == null)
            {
                _logger.LogWarning("ReplyToMessage: original message not found, id={ReplyToMessageId}", dto.ReplyToMessageId);
                return new() { IsSuccess = false, Message = "Original message not found", ErrorType = "NotFound" };
            }

            var message = BuildMessage(senderId, dto.ReceiverId, dto.Type, dto.Text, dto.File, dto.MediaDuration, dto.ReplyToMessageId);

            _unit.Message.Create(message);
            _unit.save();

            message.MediaUrl = ResolveUrl(message.MediaUrl, baseUrl);

            var payload = new
            {
                id = message.Id,
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                type = message.Type,
                textContent = message.TextContent,
                mediaUrl = message.MediaUrl,
                mediaDuration = message.MediaDuration,
                createdAt = message.CreatedAt,
                replyToMessageId = message.ReplyToMessageId,
                replyToText = originalMessage.TextContent,
                replyToType = originalMessage.Type,
                replyToMediaUrl = originalMessage.MediaUrl
            };

            await _hub.Clients.User(dto.ReceiverId).SendAsync("ReceiveMessage", payload);
            await _hub.Clients.User(senderId).SendAsync("ReceiveMessage", payload);

            _logger.LogInformation("ReplyToMessage success: messageId={MessageId}", message.Id);
            return new() { IsSuccess = true, Data = (object)payload };
        }

        // ─────────────────────────────────────────────
        // Get All Users
        // ─────────────────────────────────────────────

        public Task<ServiceResult<IEnumerable<object>>> GetUsersAsync(string currentUserId, string baseUrl)
        {
            _logger.LogInformation("GetUsers: currentUserId={UserId}", currentUserId);

            var users = _unit.User.GetALL(u => u.Id != currentUserId);

            var result = users.Select(u => (object)new
            {
                u.Id,
                u.Name,
                u.Lastseen,
                u.Email,
                ImageUrl = ResolveUrl(u.ImageURL, baseUrl),
               
            });

            return Task.FromResult(new ServiceResult<IEnumerable<object>> { IsSuccess = true, Data = result });
        }

        // ─────────────────────────────────────────────
        // Get Messages With User
        // ─────────────────────────────────────────────

        public Task<ServiceResult<IEnumerable<object>>> GetMessagesWithUserAsync(string currentUserId, string otherUserId, string baseUrl)
        {
            _logger.LogInformation("GetMessages: currentUserId={CurrentUserId}, otherUserId={OtherUserId}", currentUserId, otherUserId);

            if (IsBlocked(currentUserId, otherUserId))
            {
                _logger.LogWarning("GetMessages blocked between {CurrentUserId} and {OtherUserId}", currentUserId, otherUserId);
                return Task.FromResult(new ServiceResult<IEnumerable<object>>
                {
                    IsSuccess = false,
                    Message = "Cannot view messages with blocked user",
                    ErrorType = "Forbidden"
                });
            }

            var messages = _unit.Message.GetALL(m =>
                (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                (m.SenderId == otherUserId && m.ReceiverId == currentUserId),
                Includes: "ReplyToMessage.Sender"
            ).OrderBy(m => m.CreatedAt).ToList();

            var deletedMessages = _unit.MessageDeleted.GetALL(d =>
                d.UserId == currentUserId || d.DeletedForEveryone);

            var messageDtos = messages
                .Select(m =>
                {
                    var deletedForMe = deletedMessages.Any(d => d.MessageId == m.Id && d.UserId == currentUserId);
                    var deletedForEveryone = deletedMessages.Any(d => d.MessageId == m.Id && d.DeletedForEveryone);

                    return new
                    {
                        id = m.Id,
                        senderId = m.SenderId,
                        receiverId = m.ReceiverId,
                        type = m.Type,
                        textContent = deletedForEveryone ? "This message was deleted" : deletedForMe ? null : m.TextContent,
                        mediaUrl = deletedForEveryone || deletedForMe ? null : ResolveUrl(m.MediaUrl, baseUrl),
                        mediaDuration = m.MediaDuration,
                        createdAt = m.CreatedAt,
                        deliveredAt = m.DeliveredAt,
                        readAt = m.ReadAt,
                        isDeleted = deletedForMe || deletedForEveryone,
                        deletedForMe,
                        deletedForEveryone,
                        replyToMessageId = m.ReplyToMessageId,
                        replyToText = m.ReplyToMessage?.TextContent,
                        replyToType = m.ReplyToMessage?.Type,
                        replyToMediaUrl = m.ReplyToMessage?.MediaUrl
                    };
                })
                .Where(m => !m.deletedForMe)
                .Cast<object>();

            return Task.FromResult(new ServiceResult<IEnumerable<object>> { IsSuccess = true, Data = messageDtos });
        }

        // ─────────────────────────────────────────────
        // Mark As Delivered
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> MarkAsDeliveredAsync(string currentUserId, int messageId)
        {
            _logger.LogInformation("MarkAsDelivered: messageId={MessageId}", messageId);

            var message = _unit.Message.GetByFilter(m => m.Id == messageId);
            if (message == null)
            {
                _logger.LogWarning("MarkAsDelivered: message not found, id={MessageId}", messageId);
                return new() { IsSuccess = false, Message = "Message not found", ErrorType = "NotFound" };
            }

            if (message.DeliveredAt == null)
            {
                message.DeliveredAt = DateTime.Now;
                _unit.save();
                await _hub.Clients.User(message.SenderId).SendAsync("MessageDelivered", message.Id);
                _logger.LogInformation("MarkAsDelivered success: messageId={MessageId}", messageId);
            }

            return new() { IsSuccess = true };
        }

        // ─────────────────────────────────────────────
        // Mark All As Read (by sender)
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> MarkAsReadAsync(string currentUserId, string senderId)
        {
            _logger.LogInformation("MarkAsRead: receiverId={ReceiverId}, senderId={SenderId}", currentUserId, senderId);

            var messages = _unit.Message.GetALL(m =>
                m.SenderId == senderId &&
                m.ReceiverId == currentUserId &&
                m.ReadAt == null).ToList();

            foreach (var m in messages)
            {
                m.ReadAt = DateTime.Now;
                if (m.DeliveredAt == null)
                    m.DeliveredAt = DateTime.Now;
            }

            _unit.save();

            var ids = messages.Select(m => m.Id).ToList();
            await _hub.Clients.User(senderId).SendAsync("MessagesRead", ids);

            _logger.LogInformation("MarkAsRead: marked {Count} messages as read", messages.Count);
            return new() { IsSuccess = true, Data = (object)new { count = messages.Count } };
        }

        // ─────────────────────────────────────────────
        // Mark Single As Read
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> MarkSingleAsReadAsync(string currentUserId, int messageId)
        {
            _logger.LogInformation("MarkSingleAsRead: messageId={MessageId}", messageId);

            var message = _unit.Message.GetByFilter(m => m.Id == messageId);
            if (message == null)
            {
                _logger.LogWarning("MarkSingleAsRead: message not found, id={MessageId}", messageId);
                return new() { IsSuccess = false, Message = "Message not found", ErrorType = "NotFound" };
            }

            if (message.ReadAt == null)
            {
                message.ReadAt = DateTime.Now;
                if (message.DeliveredAt == null)
                    message.DeliveredAt = DateTime.Now;

                _unit.save();
                await _hub.Clients.User(message.SenderId).SendAsync("MessagesRead", new List<int> { message.Id });
                _logger.LogInformation("MarkSingleAsRead success: messageId={MessageId}", messageId);
            }

            return new() { IsSuccess = true };
        }

        // ─────────────────────────────────────────────
        // Unread Count
        // ─────────────────────────────────────────────

        public Task<ServiceResult<int>> GetUnreadCountAsync(string currentUserId, string senderId)
        {
            _logger.LogInformation("GetUnreadCount: receiverId={ReceiverId}, senderId={SenderId}", currentUserId, senderId);

            var count = _unit.Message.GetALL(m =>
                m.SenderId == senderId &&
                m.ReceiverId == currentUserId &&
                m.ReadAt == null).Count();

            return Task.FromResult(new ServiceResult<int> { IsSuccess = true, Data = count });
        }

        // ─────────────────────────────────────────────
        // Delete Message For Me
        // ─────────────────────────────────────────────

        public Task<ServiceResult<object>> DeleteMessageForMeAsync(string currentUserId, int messageId)
        {
            _logger.LogInformation("DeleteMessageForMe: userId={UserId}, messageId={MessageId}", currentUserId, messageId);

            var message = _unit.Message.GetByFilter(m => m.Id == messageId);
            if (message == null)
            {
                _logger.LogWarning("DeleteMessageForMe: message not found, id={MessageId}", messageId);
                return Task.FromResult(new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Message not found",
                    ErrorType = "NotFound"
                });
            }

            var alreadyDeleted = _unit.MessageDeleted.GetByFilter(d => d.MessageId == messageId && d.UserId == currentUserId);
            if (alreadyDeleted != null)
            {
                _logger.LogWarning("DeleteMessageForMe: already deleted, messageId={MessageId}", messageId);
                return Task.FromResult(new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Message already deleted",
                    ErrorType = "BadRequest"
                });
            }

            _unit.MessageDeleted.Create(new MessageDeleted
            {
                MessageId = messageId,
                UserId = currentUserId,
                DeletedAt = DateTime.Now,
                DeletedForEveryone = false
            });
            _unit.save();

            _logger.LogInformation("DeleteMessageForMe success: messageId={MessageId}", messageId);
            return Task.FromResult(new ServiceResult<object>
            {
                IsSuccess = true,
                Data = (object)new { message = "Message deleted for you", messageId }
            });
        }

        // ─────────────────────────────────────────────
        // Delete Message For Everyone
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> DeleteMessageForEveryoneAsync(string currentUserId, int messageId)
        {
            _logger.LogInformation("DeleteMessageForEveryone: userId={UserId}, messageId={MessageId}", currentUserId, messageId);

            var message = _unit.Message.GetByFilter(m => m.Id == messageId);
            if (message == null)
            {
                _logger.LogWarning("DeleteMessageForEveryone: message not found, id={MessageId}", messageId);
                return new() { IsSuccess = false, Message = "Message not found", ErrorType = "NotFound" };
            }

            if (message.SenderId != currentUserId)
            {
                _logger.LogWarning("DeleteMessageForEveryone: forbidden, userId={UserId} is not sender", currentUserId);
                return new() { IsSuccess = false, Message = "Only sender can delete message for everyone", ErrorType = "Forbidden" };
            }

            if ((DateTime.Now - message.CreatedAt).TotalMinutes > 5)
            {
                _logger.LogWarning("DeleteMessageForEveryone: time limit exceeded, messageId={MessageId}", messageId);
                return new() { IsSuccess = false, Message = "Can only delete for everyone within 5 minutes of sending", ErrorType = "BadRequest" };
            }

            _unit.MessageDeleted.Create(new MessageDeleted
            {
                MessageId = messageId,
                UserId = currentUserId,
                DeletedAt = DateTime.Now,
                DeletedForEveryone = true
            });
            _unit.save();

            await _hub.Clients.User(message.ReceiverId).SendAsync("MessageDeletedForEveryone", new
            {
                messageId,
                deletedBy = currentUserId
            });

            _logger.LogInformation("DeleteMessageForEveryone success: messageId={MessageId}", messageId);
            return new() { IsSuccess = true, Data = (object)new { message = "Message deleted for everyone", messageId } };
        }

        // ─────────────────────────────────────────────
        // Clear Chat
        // ─────────────────────────────────────────────

        public Task<ServiceResult<object>> ClearChatAsync(string currentUserId, string otherUserId)
        {
            _logger.LogInformation("ClearChat: currentUserId={CurrentUserId}, otherUserId={OtherUserId}", currentUserId, otherUserId);

            var messages = _unit.Message.GetALL(m =>
                (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                (m.SenderId == otherUserId && m.ReceiverId == currentUserId));

            int deletedCount = 0;

            foreach (var message in messages)
            {
                var exists = _unit.MessageDeleted.GetByFilter(d =>
                    d.MessageId == message.Id && d.UserId == currentUserId);

                if (exists == null)
                {
                    _unit.MessageDeleted.Create(new MessageDeleted
                    {
                        MessageId = message.Id,
                        UserId = currentUserId,
                        DeletedAt = DateTime.Now,
                        DeletedForEveryone = false
                    });
                    deletedCount++;
                }
            }

            _unit.save();

            _logger.LogInformation("ClearChat success: deletedCount={DeletedCount}", deletedCount);
            return Task.FromResult(new ServiceResult<object>
            {
                IsSuccess = true,
                Data = (object)new { message = "Chat cleared successfully", deletedCount }
            });
        }

        // ─────────────────────────────────────────────
        // Block User
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> BlockUserAsync(string blockerId, string targetUserId)
        {
            _logger.LogInformation("BlockUser: blockerId={BlockerId}, targetUserId={TargetUserId}", blockerId, targetUserId);

            var existing = _unit.BlockedUsers.GetByFilter(b =>
                b.BlockerId == blockerId && b.BlockedUserId == targetUserId);

            if (existing != null)
            {
                if (existing.IsActive)
                {
                    _logger.LogWarning("BlockUser: user already blocked, targetUserId={TargetUserId}", targetUserId);
                    return new() { IsSuccess = true, Data = (object)new { message = "User already blocked" } };
                }

                existing.IsActive = true;
                existing.BlockedAt = DateTime.Now;
                _unit.BlockedUsers.Update(existing);
                _unit.save();

                _logger.LogInformation("BlockUser: re-blocked userId={TargetUserId}", targetUserId);
                return new() { IsSuccess = true, Data = (object)new { message = "User blocked successfully" } };
            }

            _unit.BlockedUsers.Create(new BlockedUser
            {
                BlockerId = blockerId,
                BlockedUserId = targetUserId,
                BlockedAt = DateTime.Now,
                IsActive = true
            });
            _unit.save();

            await _hub.Clients.User(targetUserId).SendAsync("UserBlocked", new { blockerId });

            _logger.LogInformation("BlockUser success: targetUserId={TargetUserId}", targetUserId);
            return new() { IsSuccess = true, Data = (object)new { message = "User blocked successfully" } };
        }

        // ─────────────────────────────────────────────
        // Unblock User
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> UnblockUserAsync(string blockerId, string targetUserId)
        {
            _logger.LogInformation("UnblockUser: blockerId={BlockerId}, targetUserId={TargetUserId}", blockerId, targetUserId);

            var existing = _unit.BlockedUsers.GetByFilter(b =>
                b.BlockerId == blockerId && b.BlockedUserId == targetUserId && b.IsActive);

            if (existing == null)
            {
                _logger.LogWarning("UnblockUser: block not found for targetUserId={TargetUserId}", targetUserId);
                return new() { IsSuccess = false, Message = "Block not found", ErrorType = "NotFound" };
            }

            existing.IsActive = false;
            _unit.BlockedUsers.Update(existing);
            _unit.save();

            await _hub.Clients.User(targetUserId).SendAsync("UserUnblocked", new { blockerId });

            _logger.LogInformation("UnblockUser success: targetUserId={TargetUserId}", targetUserId);
            return new() { IsSuccess = true, Data = (object)new { message = "User unblocked successfully" } };
        }

        // ─────────────────────────────────────────────
        // Get Blocked Users
        // ─────────────────────────────────────────────

        public Task<ServiceResult<IEnumerable<object>>> GetBlockedUsersAsync(string userId, string baseUrl)
        {
            _logger.LogInformation("GetBlockedUsers: userId={UserId}", userId);

            var blockedUsers = _unit.BlockedUsers.GetALL(
                b => b.BlockerId == userId && b.IsActive,
                Includes: "BlockedUserEntity");

            var result = blockedUsers.Select(b => (object)new
            {
                id = b.BlockedUserEntity?.Id,
                name = b.BlockedUserEntity?.Name,
                email = b.BlockedUserEntity?.Email,
                imageUrl = ResolveUrl(b.BlockedUserEntity?.ImageURL, baseUrl),
                blockedAt = b.BlockedAt
            });

            return Task.FromResult(new ServiceResult<IEnumerable<object>> { IsSuccess = true, Data = result });
        }

        // ─────────────────────────────────────────────
        // Get My Chat List
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<IEnumerable<ChatListItemDTO>>> GetMyChatListAsync(string currentUserId, string baseUrl)
        {
            _logger.LogInformation("GetMyChatList: currentUserId={UserId}", currentUserId);

            var myMessages = _unit.Message.GetALL(m =>
                m.SenderId == currentUserId || m.ReceiverId == currentUserId,
                Includes: "Sender,Receiver");

            var deletedMessages = _unit.MessageDeleted.GetALL(d =>
                d.UserId == currentUserId || d.DeletedForEveryone);

            var chatUserIds = myMessages
                .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToList();

            var blockedByMe = _unit.BlockedUsers
                .GetALL(b => b.BlockerId == currentUserId && b.IsActive)
                .Select(b => b.BlockedUserId)
                .ToList();

            var chatList = new List<ChatListItemDTO>();

            foreach (var userId in chatUserIds)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) continue;

                var userMessages = myMessages
                    .Where(m =>
                        (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                        (m.SenderId == userId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.CreatedAt)
                    .ToList();

                var visibleMessages = userMessages.Where(m =>
                {
                    var deletedForMe = deletedMessages.Any(d => d.MessageId == m.Id && d.UserId == currentUserId);
                    var deletedForEveryone = deletedMessages.Any(d => d.MessageId == m.Id && d.DeletedForEveryone);
                    return !deletedForMe && !deletedForEveryone;
                }).ToList();

                var lastMessage = visibleMessages.FirstOrDefault();

                var unreadCount = visibleMessages.Count(m =>
                    m.SenderId == userId &&
                    m.ReceiverId == currentUserId &&
                    m.ReadAt == null);

                chatList.Add(new ChatListItemDTO
                {
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    ImageUrl = ResolveUrl(user.ImageURL, baseUrl),
                    UnreadCount = unreadCount,
                    IsBlockedByMe = blockedByMe.Contains(userId),
                    LastMessageText = lastMessage?.TextContent,
                    LastMessageType = lastMessage?.Type,
                    LastMessageTime = lastMessage?.CreatedAt,
                    LastMessageFromMe = lastMessage?.SenderId == currentUserId,
                    LastSeen = user.Lastseen
                });
            }

            var sorted = chatList.OrderByDescending(c => c.LastMessageTime).ToList();

            _logger.LogInformation("GetMyChatList success: {Count} chats", sorted.Count);
            return new ServiceResult<IEnumerable<ChatListItemDTO>> { IsSuccess = true, Data = sorted };
        }
    }
}
