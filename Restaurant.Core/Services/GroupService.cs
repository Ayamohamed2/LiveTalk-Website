using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NEEFRA.Core;
using NEEFRA.Core.DTO.Service;
using Realtima_Chat_project.DTO;
using Realtima_Chat_project.Models;
using Restaurant.Core.DTO.Group;
using Restaurant.Core.Models.Account;
using SignalIR_practice.Hubs;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;
namespace NEEFRA.Core.Services
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _unit;
        private readonly IHubContext<ChatHub> _hub;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GroupService> _logger;

        public GroupService(
            IUnitOfWork unit,
            IHubContext<ChatHub> hub,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            ILogger<GroupService> logger)
        {
            _unit = unit;
            _hub = hub;
            _userManager = userManager;
            _env = env;
            _logger = logger;
        }

        // ─────────────────────────────────────────────
        // Private Helpers
        // ─────────────────────────────────────────────

        private static string? ResolveUrl(string? relativeUrl, string baseUrl)
            => string.IsNullOrEmpty(relativeUrl) ? null : $"{baseUrl}/{relativeUrl}";

        private static string GenerateJoinCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task<GroupMessage> BuildGroupMessage(
            int groupId,
            string senderId,
            MessageType type,
            string? text,
            IFormFile? file,
            int? replyToId = null)
        {
            var message = new GroupMessage
            {
                GroupId = groupId,
                SenderId = senderId,
                Type = type,
                CreatedAt = DateTime.Now,
                IsForwarded = false,
                ReplyToMessageId = replyToId
            };

            if (type == MessageType.Text)
                message.TextContent = text;
            else if (file != null)
                message.MediaUrl = _unit.GroupMessages.GetImageURL(file, _env, Type: type);

            return message;
        }

        // ─────────────────────────────────────────────
        // Create Group
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> CreateGroupAsync(string userId, CreateGroupDTO dto, string baseUrl)
        {
            _logger.LogInformation("CreateGroup: userId={UserId}, groupName={GroupName}", userId, dto.Name);

            string? imgUrl = null;
            if (dto.Image != null)
                imgUrl = _unit.Group.GetImageURL(dto.Image, userId, _env);

            var joinCode = GenerateJoinCode();
            while (_unit.Group.GetByFilter(j => j.JoinCode == joinCode) != null)
                joinCode = GenerateJoinCode();

            var group = new Group
            {
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = imgUrl,
                CreatorId = userId,
                JoinCode = joinCode,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _unit.Group.Create(group);
            _unit.save();

            var created = _unit.Group.GetByFilter(g => g.JoinCode == joinCode && g.Name == dto.Name && g.CreatorId == userId);
            if (created == null)
            {
                return new() { IsSuccess = false, Message = "", ErrorType = "NotFound" };


            }

            _unit.GroupMember.Create(new GroupMember
            {
                GroupId = created.Id,
                UserId = userId,
                IsAdmin = true,
                JoinedAt = DateTime.Now,
                IsActive = true
            });
            _unit.save();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("CreateGroup: user not found, userId={UserId}", userId);
                return new() { IsSuccess = false, Message = "User not found", ErrorType = "NotFound" };
            }

            _logger.LogInformation("CreateGroup success: groupId={GroupId}", created.Id);
            return new()
            {
                IsSuccess = true,
                Data = (object)new
                {
                    id = created.Id,
                    name = created.Name,
                    description = created.Description,
                    imageUrl = ResolveUrl(created.ImageUrl, baseUrl),
                    joinCode = created.JoinCode,
                    creatorName = user.Name,
                    createdAt = created.CreatedAt
                }
            };
        }

        // ─────────────────────────────────────────────
        // Join Group
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> JoinGroupAsync(string userId, JoinGroupDTO dto, string baseUrl)
        {
            _logger.LogInformation("JoinGroup: userId={UserId}, joinCode={JoinCode}", userId, dto.JoinCode);

            var group = _unit.Group.GetByFilter(g => g.JoinCode == dto.JoinCode);
            if (group == null)
            {
                _logger.LogWarning("JoinGroup: group not found, joinCode={JoinCode}", dto.JoinCode);
                return new() { IsSuccess = false, Message = "Group not found", ErrorType = "NotFound" };
            }

            var isMember = _unit.GroupMember.GetALL(m => m.GroupId == group.Id && m.UserId == userId && m.IsActive).Any();
            if (isMember)
                return new() { IsSuccess = false, Message = "You are already a member of this group", ErrorType = "BadRequest" };

            var wasMember = _unit.GroupMember.GetALL(m => m.GroupId == group.Id && m.UserId == userId && !m.IsActive).Any();
            GroupMember groupMember;

            if (wasMember)
            {
                groupMember = _unit.GroupMember.GetByFilter(m => m.GroupId == group.Id && m.UserId == userId && !m.IsActive);
                groupMember.IsActive = true;
                _unit.GroupMember.Update(groupMember);
            }
            else
            {
                groupMember = new GroupMember
                {
                    GroupId = group.Id,
                    UserId = userId,
                    IsAdmin = false,
                    JoinedAt = DateTime.Now,
                    IsActive = true
                };
                _unit.GroupMember.Create(groupMember);
            }

            _unit.save();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("JoinGroup: user not found, userId={UserId}", userId);
                return new() { IsSuccess = false, Message = "User not found", ErrorType = "NotFound" };
            }

            await _hub.Clients.Group($"Group{group.Id}").SendAsync("MemberJoined", new
            {
                groupId = group.Id,
                userId,
                userName = user.Name,
                userImage = ResolveUrl(user.ImageURL, baseUrl),
                joinedAt = groupMember.JoinedAt
            });

            _logger.LogInformation("JoinGroup success: groupId={GroupId}", group.Id);
            return new() { IsSuccess = true, Data = (object)new { groupId = group.Id, groupName = group.Name } };
        }

        // ─────────────────────────────────────────────
        // Add Members
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> AddMembersAsync(string userId, AddMembersDTO dto, string baseUrl)
        {
            _logger.LogInformation("AddMembers: adminId={UserId}, groupId={GroupId}", userId, dto.GroupId);

            var isAdmin = _unit.GroupMember.GetByFilter(g => g.GroupId == dto.GroupId && g.UserId == userId && g.IsActive)?.IsAdmin;
            if (isAdmin != true)
            {
                _logger.LogWarning("AddMembers: forbidden, userId={UserId}", userId);
                return new() { IsSuccess = false, Message = "Only admins can add members", ErrorType = "Forbidden" };
            }

            var group = _unit.Group.GetByFilter(u => u.Id == dto.GroupId);
            var addedMembers = new List<object>();

            foreach (var memberId in dto.UserIds)
            {
                var alreadyMember = _unit.GroupMember.GetALL(m => m.GroupId == group.Id && m.UserId == memberId && m.IsActive).Any();
                if (alreadyMember) continue;

                var wasMember = _unit.GroupMember.GetALL(m => m.GroupId == group.Id && m.UserId == memberId && !m.IsActive).Any();
                if (wasMember)
                {
                    var existing = _unit.GroupMember.GetByFilter(m => m.GroupId == group.Id && m.UserId == memberId && !m.IsActive);
                    existing.IsActive = true;
                    _unit.GroupMember.Update(existing);
                }
                else
                {
                    _unit.GroupMember.Create(new GroupMember
                    {
                        GroupId = group.Id,
                        UserId = memberId,
                        IsAdmin = false,
                        JoinedAt = DateTime.Now,
                        IsActive = true
                    });
                }

                _unit.save();

                var member = await _userManager.FindByIdAsync(memberId);
                if (member != null)
                {
                    addedMembers.Add(new
                    {
                        userId = member.Id,
                        userName = member.Name,
                        userImage = ResolveUrl(member.ImageURL, baseUrl)
                    });
                }
            }

            if (addedMembers.Any())
            {
                await _hub.Clients.Group($"Group{dto.GroupId}").SendAsync("MembersAdded", new
                {
                    groupId = dto.GroupId,
                    members = addedMembers,
                    addedBy = userId
                });
            }

            _logger.LogInformation("AddMembers success: {Count} added to groupId={GroupId}", addedMembers.Count, dto.GroupId);
            return new() { IsSuccess = true, Data = (object)new { message = $"{addedMembers.Count} member(s) added successfully", members = addedMembers } };
        }

        // ─────────────────────────────────────────────
        // Send Message
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> SendMessageAsync(string senderId, SendGroupMessageDTO dto, string baseUrl)
        {
            _logger.LogInformation("SendGroupMessage: senderId={SenderId}, groupId={GroupId}", senderId, dto.GroupId);

            var isMember = _unit.GroupMember.GetALL(m => m.GroupId == dto.GroupId && m.UserId == senderId && m.IsActive).Any();
            if (!isMember)
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "BadRequest" };

            var message = await BuildGroupMessage(dto.GroupId, senderId, dto.Type, dto.Text, dto.File);

            _unit.GroupMessages.Create(message);
            _unit.save();

            message.MediaUrl = ResolveUrl(message.MediaUrl, baseUrl);

            var sender = await _userManager.FindByIdAsync(senderId);
            var allMembers = _unit.GroupMember.GetALL(m => m.GroupId == dto.GroupId);
            var activeMembers = allMembers.Count(m => m.UserId != senderId && m.IsActive);

            var payload = new
            {
                id = message.Id,
                groupId = message.GroupId,
                senderId = message.SenderId,
                senderName = sender?.Name,
                senderImage = ResolveUrl(sender?.ImageURL, baseUrl),
                type = message.Type,
                textContent = message.TextContent,
                mediaUrl = message.MediaUrl,
                mediaDuration = message.MediaDuration,
                createdAt = message.CreatedAt,
                isForwarded = message.IsForwarded,
                totalMembers = activeMembers,
                readByCount = 0,
                readByMe = false,
                readByAll = false
            };

            await _hub.Clients.Group($"Group{dto.GroupId}").SendAsync("ReceiveGroupMessage", payload);

            _logger.LogInformation("SendGroupMessage success: messageId={MessageId}", message.Id);
            return new() { IsSuccess = true, Data = (object)payload };
        }

        // ─────────────────────────────────────────────
        // Reply To Message
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> ReplyToMessageAsync(string senderId, ReplyToMessageDTO dto, string baseUrl)
        {
            _logger.LogInformation("ReplyToGroupMessage: senderId={SenderId}, replyTo={ReplyToMessageId}", senderId, dto.ReplyToMessageId);

            var isMember = _unit.GroupMember.GetALL(m => m.GroupId == dto.GroupId && m.UserId == senderId && m.IsActive).Any();
            if (!isMember)
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "BadRequest" };

            var original = _unit.GroupMessages.GetByFilter(m => m.Id == dto.ReplyToMessageId, Includes: "Sender");
            if (original == null)
            {
                _logger.LogWarning("ReplyToGroupMessage: original not found, id={ReplyToMessageId}", dto.ReplyToMessageId);
                return new() { IsSuccess = false, Message = "Message not found", ErrorType = "NotFound" };
            }

            var message = await BuildGroupMessage(dto.GroupId, senderId, dto.Type, dto.Text, dto.File, dto.ReplyToMessageId);

            _unit.GroupMessages.Create(message);
            _unit.save();

            message.MediaUrl = ResolveUrl(message.MediaUrl, baseUrl);

            var sender = await _userManager.FindByIdAsync(senderId);
            var allMembers = _unit.GroupMember.GetALL(m => m.GroupId == dto.GroupId);
            var activeMembers = allMembers.Count(m => m.UserId != senderId && m.IsActive);

            var payload = new
            {
                id = message.Id,
                groupId = message.GroupId,
                senderId = message.SenderId,
                senderName = sender?.Name,
                senderImage = ResolveUrl(sender?.ImageURL, baseUrl),
                type = message.Type,
                textContent = message.TextContent,
                mediaUrl = message.MediaUrl,
                mediaDuration = message.MediaDuration,
                createdAt = message.CreatedAt,
                isForwarded = false,
                replyToMessageId = message.ReplyToMessageId,
                replyToText = original.TextContent,
                replyToSenderName = original.Sender?.Name,
                replyToType = original.Type,
                replyToMediaUrl = original.MediaUrl,
                totalMembers = activeMembers,
                readByCount = 0,
                readByMe = false,
                readByAll = false
            };

            await _hub.Clients.Group($"Group{dto.GroupId}").SendAsync("ReceiveGroupMessage", payload);

            _logger.LogInformation("ReplyToGroupMessage success: messageId={MessageId}", message.Id);
            return new() { IsSuccess = true, Data = (object)payload };
        }

        // ─────────────────────────────────────────────
        // Mark As Read
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> MarkAsReadAsync(string userId, int messageId)
        {
            _logger.LogInformation("MarkGroupMessageAsRead: userId={UserId}, messageId={MessageId}", userId, messageId);

            var message = _unit.GroupMessages.GetByFilter(m => m.Id == messageId);
            if (message == null)
                return new() { IsSuccess = false, Message = "Message not found", ErrorType = "NotFound" };

            var isMember = _unit.GroupMember.GetALL(m => m.GroupId == message.GroupId && m.UserId == userId && m.IsActive).Any();
            if (!isMember)
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "BadRequest" };

            var existingRead = _unit.GroupMessageRead.GetByFilter(r => r.MessageId == messageId && r.UserId == userId);
            if (existingRead != null)
                return new() { IsSuccess = true, Data = (object)new { message = "Message already read" } };

            _unit.GroupMessageRead.Create(new GroupMessageRead
            {
                MessageId = messageId,
                UserId = userId,
                ReadAt = DateTime.Now
            });
            _unit.save();

            var user = await _userManager.FindByIdAsync(userId);

            await _hub.Clients.User(message.SenderId).SendAsync("MessageRead", new
            {
                messageId,
                groupId = message.GroupId,
                userId,
                userName = user?.Name,
                readAt = DateTime.Now
            });

            _logger.LogInformation("MarkGroupMessageAsRead success: messageId={MessageId}", messageId);
            return new() { IsSuccess = true, Data = (object)new { message = "Message marked as read" } };
        }

        // ─────────────────────────────────────────────
        // Who Read
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<IEnumerable<object>>> WhoReadAsync(string userId, int messageId, string baseUrl)
        {
            _logger.LogInformation("WhoReadGroupMessage: userId={UserId}, messageId={MessageId}", userId, messageId);

            var message = _unit.GroupMessages.GetByFilter(m => m.Id == messageId);
            if (message == null)
                return new() { IsSuccess = false, Message = "Message not found", ErrorType = "NotFound" };

            var isMember = _unit.GroupMember.GetALL(m => m.GroupId == message.GroupId && m.UserId == userId && m.IsActive).Any();
            if (!isMember)
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "BadRequest" };

            var reads = _unit.GroupMessageRead.GetALL(r => r.MessageId == messageId);
            var allMembers = _unit.GroupMember.GetALL(m => m.GroupId == message.GroupId && m.IsActive, Includes: "User");

            var result = allMembers
                .Where(m => m.UserId != userId)
                .Select(m =>
                {
                    var read = reads.FirstOrDefault(r => r.UserId == m.UserId);
                    return (object)new
                    {
                        userId = m.UserId,
                        name = m.User?.Name,
                        imageUrl = ResolveUrl(m.User?.ImageURL, baseUrl),
                        isRead = read != null,
                        readAt = read?.ReadAt
                    };
                });

            return new() { IsSuccess = true, Data = result };
        }

        // ─────────────────────────────────────────────
        // Get Group Messages
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<List<object>>> GetGroupMessagesAsync(string userId, int groupId, string baseUrl)
        {
            _logger.LogInformation("GetGroupMessages: userId={UserId}, groupId={GroupId}", userId, groupId);

            var member = _unit.GroupMember.GetByFilter(m => m.GroupId == groupId && m.UserId == userId && m.IsActive);
            if (member == null)
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "BadRequest" };

      
            var messages = _unit.GroupMessages.GetALL(m => m.GroupId == groupId && m.CreatedAt >= member.JoinedAt);
            var deletedMessages =_unit.GroupMessageDeleted.GetALL(d => d.UserId == userId || d.DeletedForEveryone);
            var allMembers = _unit.GroupMember.GetALL(m => m.GroupId == groupId && m.IsActive);
            var group = _unit.Group.GetByFilter(u => u.Id == groupId);

            var memberIds = allMembers.Select(m => m.UserId);
            var users = _unit.User.GetALL(u => memberIds.Contains(u.Id));
            var usersDict = users.ToDictionary(u => u.Id, u => u);

            var replyToIds = messages.Select(m => m.ReplyToMessageId);
            var replyToMessages =_unit.GroupMessages.GetALL(m => replyToIds.Contains(m.Id));
            var replyToDict = replyToMessages.ToDictionary(m => m.Id, m => m);

            var groupImageUrl = string.IsNullOrEmpty(group.ImageUrl) ? null : $"{baseUrl}/{group.ImageUrl}";

            var data = messages.Select(m =>
            {
                var replyTo = new GroupMessage();
                usersDict.TryGetValue(m.SenderId, out var sender);
                if (m.ReplyToMessageId != null)
                    replyToDict.TryGetValue((int)m.ReplyToMessageId, out replyTo);

                var deletedForMe = deletedMessages.Any(d => d.MessageId == m.Id && d.UserId == userId);
                var deletedForEveryone = deletedMessages.Any(d => d.MessageId == m.Id && d.DeletedForEveryone);
                var activeMembersCount = allMembers.Count(mem => mem.UserId != m.SenderId && mem.IsActive);

                return new
                {
                    id = m.Id,
                    groupId = m.GroupId,
                    groupName = group.Name,
                    groupImage = groupImageUrl,
                    senderId = m.SenderId,
                    senderName = sender?.Name,
                    senderImage = string.IsNullOrEmpty(sender?.ImageURL) ? null : baseUrl + sender.ImageURL,
                    type = m.Type,
                    textContent = deletedForEveryone ? "This message was deleted" : deletedForMe ? null : m.TextContent,
                    mediaUrl = deletedForEveryone || deletedForMe ? null : string.IsNullOrEmpty(m.MediaUrl) ? null : $"{baseUrl}/{m.MediaUrl}",
                    mediaDuration = m.MediaDuration,
                    createdAt = m.CreatedAt,
                    totalMembers = activeMembersCount,
                    isDeleted = deletedForMe || deletedForEveryone,
                    deletedForMe,
                    deletedForEveryone,
                    replyToMessageId = m.ReplyToMessageId,
                    replyToText = replyTo?.TextContent,
                    replyToSenderName = replyTo?.SenderId != null &&
                                        usersDict.TryGetValue(replyTo.SenderId, out var rSender)
                                        ? rSender?.Name : null,
                    replyToType = replyTo?.Type,
                    replyToMediaUrl = replyTo?.MediaUrl
                };
            })
            .Where(m => !m.deletedForMe)
            .Select(m => (object)m)
            .ToList();

            _logger.LogInformation("Fetched {Count} messages for groupId: {GroupId}", data.Count, groupId);

            var result = new ServiceResult<List<object>> { IsSuccess = true, Data = data };
            return result;
        }

        // ─────────────────────────────────────────────
        // Get Group Members
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<IEnumerable<object>>> GetGroupMembersAsync(string userId, int groupId, string baseUrl)
        {
            _logger.LogInformation("GetGroupMembers: userId={UserId}, groupId={GroupId}", userId, groupId);

            var isMember = _unit.GroupMember.GetALL(m => m.GroupId == groupId && m.UserId == userId && m.IsActive).Any();
            if (!isMember)
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "BadRequest" };

            var members = _unit.GroupMember.GetALL(m => m.GroupId == groupId && m.IsActive, Includes: "User");

            var result = members.Select(m => (object)new
            {
                userId = m.UserId,
                name = m.User?.Name,
                imageUrl = ResolveUrl(m.User?.ImageURL, baseUrl),
                joinedAt = m.JoinedAt,
                isAdmin = m.IsAdmin
            });

            return new() { IsSuccess = true, Data = result };
        }

        // ─────────────────────────────────────────────
        // Remove Member
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> RemoveMemberAsync(string userId, RemoveMemberDTO dto)
        {
            _logger.LogInformation("RemoveMember: adminId={UserId}, targetUserId={TargetUserId}, groupId={GroupId}", userId, dto.UserId, dto.GroupId);

            var isAdmin = _unit.GroupMember.GetByFilter(g => g.GroupId == dto.GroupId && g.UserId == userId && g.IsActive)?.IsAdmin;
            if (isAdmin != true)
                return new() { IsSuccess = false, Message = "Only admins can remove members", ErrorType = "Forbidden" };

            var group = _unit.Group.GetByFilter(g => g.Id == dto.GroupId);
            if (group?.CreatorId == dto.UserId)
                return new() { IsSuccess = false, Message = "Cannot remove group creator", ErrorType = "BadRequest" };

            var member = _unit.GroupMember.GetByFilter(gm => gm.GroupId == dto.GroupId && gm.UserId == dto.UserId);
            if (member == null)
                return new() { IsSuccess = false, Message = "Member not found", ErrorType = "NotFound" };

            member.IsActive = false;
            _unit.GroupMember.Update(member);
            _unit.save();

            var removedUser = await _userManager.FindByIdAsync(dto.UserId);

            await _hub.Clients.Group($"Group{dto.GroupId}").SendAsync("MemberRemoved", new
            {
                groupId = dto.GroupId,
                userId = dto.UserId,
                removedBy = userId,
                userName = removedUser?.Name
            });

            await _hub.Clients.User(dto.UserId).SendAsync("RemovedFromGroup", new
            {
                groupId = dto.GroupId,
                groupName = group?.Name,
                userName = removedUser?.Name
            });

            _logger.LogInformation("RemoveMember success: targetUserId={TargetUserId} from groupId={GroupId}", dto.UserId, dto.GroupId);
            return new() { IsSuccess = true, Data = (object)new { message = "Member removed successfully" } };
        }

        // ─────────────────────────────────────────────
        // Leave Group
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> LeaveGroupAsync(string userId, int groupId)
        {
            _logger.LogInformation("LeaveGroup: userId={UserId}, groupId={GroupId}", userId, groupId);

            var group = _unit.Group.GetByFilter(g => g.Id == groupId);
            if (group?.CreatorId == userId)
                return new() { IsSuccess = false, Message = "Group creator cannot leave", ErrorType = "BadRequest" };

            var member = _unit.GroupMember.GetByFilter(gm => gm.GroupId == groupId && gm.UserId == userId);
            if (member == null)
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "NotFound" };

            member.IsActive = false;
            _unit.save();

            var user = await _userManager.FindByIdAsync(userId);

            await _hub.Clients.Group($"Group{groupId}").SendAsync("MemberLeft", new
            {
                groupId,
                userId,
                userName = user?.Name
            });

            _logger.LogInformation("LeaveGroup success: userId={UserId}, groupId={GroupId}", userId, groupId);
            return new() { IsSuccess = true, Data = (object)new { message = "Left group successfully" } };
        }

        // ─────────────────────────────────────────────
        // Get Unread Count
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<int>> GetUnreadCountAsync(string userId, int groupId)
        {
            _logger.LogInformation("Fetching unread count – groupId: {GroupId}, userId: {UserId}", groupId, userId);

            var isMember = (_unit.GroupMember.GetALL(m => m.GroupId == groupId && m.UserId == userId && m.IsActive));
            if (isMember==null || !isMember.Any())
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "BadRequest" };

            var member = _unit.GroupMember.GetByFilter(g => g.GroupId == groupId && g.UserId == userId && g.IsActive);
            if (member == null)
                return new() { IsSuccess = false, Message = "Member not found", ErrorType = "NotFound" };

            var messages = _unit.GroupMessages.GetALL(g => g.GroupId == groupId && g.SenderId != userId && g.CreatedAt > member.JoinedAt);
            var messageIds = messages?.Select(m => m.Id).ToList();
            var reads = _unit.GroupMessageRead.GetALL(r => r.UserId == userId && messageIds.Contains(r.MessageId));
            var readsIds = reads?.Select(r => r.MessageId).ToHashSet();
          
            var unreadCount = messageIds.Count(id => !readsIds.Contains(id));
            _logger.LogInformation("Unread count for userId: {UserId}, groupId: {GroupId} = {UnreadCount}", userId, groupId, unreadCount);
            return new() { IsSuccess = true, Data = unreadCount  };
        }

        // ─────────────────────────────────────────────
        // Get Group By Id
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> GetGroupByIdAsync(string userId, int groupId, string baseUrl)
        {
            _logger.LogInformation("GetGroupById: userId={UserId}, groupId={GroupId}", userId, groupId);

            var group = _unit.Group.GetByFilter(g => g.Id == groupId, Includes: "Creator");
            if (group == null)
                return new() { IsSuccess = false, Message = "Group not found", ErrorType = "NotFound" };

            var membersCount = _unit.GroupMember.GetALL(m => m.GroupId == groupId && m.IsActive).Count();

            _logger.LogInformation("GetGroupById success: groupId={GroupId}", groupId);
            return new()
            {
                IsSuccess = true,
                Data = (object)new
                {
                    id = group.Id,
                    name = group.Name,
                    description = group.Description,
                    imageUrl = ResolveUrl(group.ImageUrl, baseUrl),
                    joinCode = group.JoinCode,
                    creatorId = group.CreatorId,
                    creatorName = group.Creator?.Name,
                    createdAt = group.CreatedAt,
                    membersCount
                }
            };
        }

        // ─────────────────────────────────────────────
        // Update Group
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> UpdateGroupAsync(string userId, int groupId, UpdateGroupDTO dto, string baseUrl)
        {
            _logger.LogInformation("UpdateGroup: userId={UserId}, groupId={GroupId}", userId, groupId);

            var isAdmin = _unit.GroupMember.GetByFilter(m => m.GroupId == groupId && m.UserId == userId && m.IsActive)?.IsAdmin ?? false;
            if (!isAdmin)
                return new() { IsSuccess = false, Message = "Only admins can update group info", ErrorType = "Forbidden" };

            var group = _unit.Group.GetByFilter(g => g.Id == groupId);
            if (group == null)
                return new() { IsSuccess = false, Message = "Group not found", ErrorType = "NotFound" };

            if (!string.IsNullOrEmpty(dto.Name))
                group.Name = dto.Name;

            group.Description = dto.Description;

            if (dto.Image != null)
                group.ImageUrl = _unit.Group.GetImageURL(dto.Image, userId, _env);

            _unit.Group.Update(group);
            _unit.save();

            await _hub.Clients.Group($"Group{groupId}").SendAsync("GroupInfoUpdated", new
            {
                groupId,
                name = group.Name,
                description = group.Description,
                imageUrl = ResolveUrl(group.ImageUrl, baseUrl),
                updatedBy = userId
            });

            _logger.LogInformation("UpdateGroup success: groupId={GroupId}", groupId);
            return new()
            {
                IsSuccess = true,
                Data = (object)new
                {
                    message = "Group updated successfully",
                    group = new { id = group.Id, name = group.Name, description = group.Description, imageUrl = ResolveUrl(group.ImageUrl, baseUrl) }
                }
            };
        }

        // ─────────────────────────────────────────────
        // Delete Message For Me
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> DeleteMessageForMeAsync(string userId, int messageId)
        {
            _logger.LogInformation("DeleteGroupMessageForMe: userId={UserId}, messageId={MessageId}", userId, messageId);

            var message = _unit.GroupMessages.GetByFilter(m => m.Id == messageId);
            if (message == null)
                return new() { IsSuccess = false, Message = "Message not found", ErrorType = "NotFound" };

            var isMember = _unit.GroupMember.GetALL(m => m.GroupId == message.GroupId && m.UserId == userId && m.IsActive);

            
            if ( (isMember==null))
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "BadRequest" };
            var iss = isMember.Any();
            if ((!iss))
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "BadRequest" };

            var alreadyDeleted = _unit.GroupMessageDeleted.GetByFilter(d => d.MessageId == messageId && d.UserId == userId);
            if (alreadyDeleted != null)
                return new() { IsSuccess = true, Data = (object)new { message = "Message already deleted for you" } };

            _unit.GroupMessageDeleted.Create(new GroupMessageDeleted
            {
                MessageId = messageId,
                UserId = userId,
                DeletedAt = DateTime.Now,
                DeletedForEveryone = false
            });
            _unit.save();

            _logger.LogInformation("DeleteGroupMessageForMe success: messageId={MessageId}", messageId);
            return new() { IsSuccess = true, Data = (object)new { message = "Message deleted for you", messageId } };
        }

        // ─────────────────────────────────────────────
        // Delete Message For Everyone
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> DeleteMessageForEveryoneAsync(string userId, int messageId)
        {
            _logger.LogInformation("DeleteGroupMessageForEveryone: userId={UserId}, messageId={MessageId}", userId, messageId);

            var message = _unit.GroupMessages.GetByFilter(m => m.Id == messageId);
            if (message == null)
                return new() { IsSuccess = false, Message = "Message not found", ErrorType = "NotFound" };

            var member = _unit.GroupMember.GetByFilter(m => m.GroupId == message.GroupId && m.UserId == userId && m.IsActive);
            if (member == null)
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "BadRequest" };

            var canDelete = member.IsAdmin || message.SenderId == userId;
            if (!canDelete)
                return new() { IsSuccess = false, Message = "Only message sender or admins can delete for everyone", ErrorType = "Forbidden" };

            if (message.SenderId == userId && !member.IsAdmin && (DateTime.Now - message.CreatedAt).TotalMinutes > 5)
                return new() { IsSuccess = false, Message = "Can only delete for everyone within 5 minutes of sending", ErrorType = "BadRequest" };

            _unit.GroupMessageDeleted.Create(new GroupMessageDeleted
            {
                MessageId = messageId,
                UserId = userId,
                DeletedAt = DateTime.Now,
                DeletedForEveryone = true
            });
            _unit.save();

            await _hub.Clients.Group($"Group{message.GroupId}").SendAsync("MessageDeletedForEveryone", new
            {
                messageId,
                groupId = message.GroupId,
                deletedBy = userId
            });

            _logger.LogInformation("DeleteGroupMessageForEveryone success: messageId={MessageId}", messageId);
            return new() { IsSuccess = true, Data = (object)new { message = "Message deleted for everyone", messageId } };
        }

        // ─────────────────────────────────────────────
        // Clear Chat
        // ─────────────────────────────────────────────


        public async Task<ServiceResult<object>> ClearChatAsync(string userId, int groupId)
        {
            _logger.LogInformation("Clearing chat – groupId: {GroupId}, userId: {UserId}", groupId, userId);

            var isMember = (_unit.GroupMember.GetALL(m => m.GroupId == groupId && m.UserId == userId && m.IsActive)).Any();
            if (!isMember)
                return new() { IsSuccess = false, Message = "You are not a member of this group", ErrorType = "BadRequest" };

            var member = _unit.GroupMember.GetByFilter(m => m.GroupId == groupId && m.UserId == userId && m.IsActive);
            var messages = _unit.GroupMessages.GetALL(m => m.GroupId == groupId && m.CreatedAt >= member.JoinedAt);
            var alreadyDeleted = (_unit.GroupMessageDeleted.GetALL(d => d.UserId == userId)).ToDictionary(m => m.MessageId);

            int deletedCount = 0;
            foreach (var msg in messages)
            {
                if (!alreadyDeleted.ContainsKey(msg.Id))
                {
                    _unit.GroupMessageDeleted.Create(new GroupMessageDeleted
                    {
                        MessageId = msg.Id,
                        UserId = userId,
                        DeletedAt = DateTime.UtcNow,
                        DeletedForEveryone = false
                    });
                    deletedCount++;
                }
            }
         

            _logger.LogInformation("Chat cleared – userId: {UserId}, groupId: {GroupId}, deletedCount: {Count}", userId, groupId, deletedCount);
            return new() { IsSuccess = true, Data = new { message = "Chat cleared successfully", deletedCount } };
        }
            // ─────────────────────────────────────────────
            // Get Available Users
            // ─────────────────────────────────────────────

            public async Task<ServiceResult<IEnumerable<object>>> GetAvailableUsersAsync(string userId, int groupId, string baseUrl)
        {
            _logger.LogInformation("GetAvailableUsers: adminId={UserId}, groupId={GroupId}", userId, groupId);

            var isAdmin = _unit.GroupMember.GetByFilter(g => g.GroupId == groupId && g.UserId == userId && g.IsActive)?.IsAdmin;
            if (isAdmin != true)
                return new() { IsSuccess = false, Message = "Only admins can view available users", ErrorType = "Forbidden" };

            var allUsers = _unit.User.GetALL().ToList();
            var existingMembers = _unit.GroupMember.GetALL(m => m.GroupId == groupId && m.IsActive).Select(m => m.UserId).ToHashSet();

            var result = allUsers
                .Where(u => !existingMembers.Contains(u.Id))
                .Select(u => (object)new
                {
                    userId = u.Id,
                    name = u.Name,
                    email = u.Email,
                    imageUrl = ResolveUrl(u.ImageURL, baseUrl)
                });

            return new() { IsSuccess = true, Data = result };
        }

        // ─────────────────────────────────────────────
        // Get My Groups
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<List<object>>>   GetMyGroupsAsync(string userId, string baseUrl)
        {
            _logger.LogInformation("GetMyGroups: userId={UserId}", userId);

            var groups = _unit.GroupMember.GetALL(g => g.UserId == userId && g.IsActive, Includes: "Group");
            var groupIds = groups.Select(g => g.GroupId).ToList();

            var allMembers = _unit.GroupMember.GetALL(m => groupIds.Contains(m.GroupId) && m.IsActive);
            var deletedMessages = _unit.GroupMessageDeleted.GetALL(d => d.UserId == userId || d.DeletedForEveryone);

            var groupDTOs = new List<object>();

            foreach (var group in groups)
            {
                var member = _unit.GroupMember.GetByFilter(gm => gm.GroupId == group.GroupId && gm.UserId == userId && gm.IsActive);
                if (member == null) continue;

                var messages = _unit.GroupMessages.GetALL(
                    m => m.GroupId == group.GroupId && m.CreatedAt >= member.JoinedAt,
                    Includes: "Sender");

                var groupMembers = allMembers.Where(m => m.GroupId == group.GroupId).ToList();
                var membersCount = groupMembers.Count;
                var isAdmin = groupMembers.FirstOrDefault(m => m.UserId == userId)?.IsAdmin ?? false;

                var visibleMessages = messages.Select(m =>
                {
                    var deletedForMe = deletedMessages.Any(d => d.MessageId == m.Id && d.UserId == userId);
                    var deletedForEveryone = deletedMessages.Any(d => d.MessageId == m.Id && d.DeletedForEveryone);

                    return new
                    {
                        sender = m.Sender,
                        type = m.Type,
                        textContent = deletedForEveryone ? "This message was deleted" : deletedForMe ? null : m.TextContent,
                        mediaUrl = deletedForEveryone || deletedForMe ? null : ResolveUrl(m.MediaUrl, baseUrl),
                        createdAt = m.CreatedAt,
                        deletedForMe
                    };
                }).Where(m => !m.deletedForMe).ToList();

                var lastMessage = visibleMessages.LastOrDefault();

                groupDTOs.Add(new
                {
                    id = group.GroupId,
                    name = group.Group.Name,
                    description = group.Group.Description,
                    imageUrl = ResolveUrl(group.Group.ImageUrl, baseUrl),
                    joinCode = group.Group.JoinCode,
                    creatorName = group.Group.Creator?.Name,
                    membersCount,
                    isAdmin,
                    lastMessageCreatedAt = lastMessage?.createdAt,
                    lastMessage = lastMessage != null ? (object)new
                    {
                        text = lastMessage.textContent,
                        senderName = lastMessage.sender?.Name,
                        createdAt = lastMessage.createdAt,
                        type = lastMessage.type
                    } : null
                });
            }

            var sorted = groupDTOs
                .OrderByDescending(g => ((dynamic)g).lastMessageCreatedAt)
                .ToList();

            _logger.LogInformation("GetMyGroups success: {Count} groups for userId={UserId}", sorted.Count, userId);
            return new() { IsSuccess = true, Data = sorted };
        }

        

     
    }
}
