using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Realtima_Chat_project.Models;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Realtima_Chat_project.DataAccess.Reposatory;
using Microsoft.AspNetCore.Components.Forms;

namespace SignalIR_practice.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IUnitOfWork unit;

        private static readonly ConcurrentDictionary<string, HashSet<string>> Connections
            = new ConcurrentDictionary<string, HashSet<string>>();
        private static readonly ConcurrentDictionary<string, HashSet<int>> UserGroups = new();
        private static readonly ConcurrentDictionary<int, HashSet<string>> GroupTypingUsers = new();

        public ChatHub(IUnitOfWork unit)
        {
            this.unit = unit;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            if (string.IsNullOrEmpty(userId))
                return;

            var userConnections = Connections.GetOrAdd(userId, _ => new HashSet<string>());

            lock (userConnections)
            {
                userConnections.Add(connectionId);
            }


            var users = unit.User.GetALL().Select(u => new
            {
                UserId = u.Id,
                IsOnline = Connections.ContainsKey(u.Id),
                LastSeen = u.Lastseen
            }).ToList();

            await Clients.Caller.SendAsync("InitialUserStatuses", users);

            
            if (userConnections.Count == 1)
            {
                await Clients.Others.SendAsync("UserOnline", userId);
            }
            if (!UserGroups.ContainsKey(userId))
            {
                UserGroups[userId] = new HashSet<int>();
            }
            var groups = unit.GroupMember.GetALL(g => g.UserId == userId).Select(g => g.GroupId).ToList();

            foreach(var g in groups)
            {
                var groupName = "Group" + g;

                await Groups.AddToGroupAsync(connectionId, groupName);
                lock (UserGroups)
                {
                    UserGroups[userId].Add(g);
                }

            }

            await base.OnConnectedAsync();
        }

        
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            if (string.IsNullOrEmpty(userId))
                return;

            if (Connections.TryGetValue(userId, out var userConnections))
            {
                lock (userConnections)
                {
                    userConnections.Remove(connectionId);
                }


                if (userConnections.Count == 0)
                {
                    Connections.TryRemove(userId, out _);

                    var user = unit.User.GetByFilter(u => u.Id == userId);
                    var lastSeen = DateTime.Now;

                    if (user != null)
                    {
                        user.Lastseen = lastSeen;
                        unit.save();
                    }

                    await Clients.All.SendAsync("Useroffline", userId, lastSeen);
                }
            }
            if (UserGroups.TryGetValue(userId, out var groups))
            {
                foreach (var groupId in groups)
                {
                    var groupName = $"Group_{groupId}";


                    if (GroupTypingUsers.ContainsKey(groupId))
                    {
                        GroupTypingUsers[groupId].Remove(userId);
                        await Clients.OthersInGroup(groupName).SendAsync("UserStoppedTyping", new
                        {
                            groupId = groupId,
                            userId = userId
                        });
                    }

                }

                UserGroups.Remove(userId,out _);
            }
          

            await base.OnDisconnectedAsync(exception);
        }

        public async Task Typing(string receiverId)
        {
            var senderId = Context.UserIdentifier;
            var sender = unit.User.GetByFilter(u => u.Id == senderId);

            if (sender != null)
            {
                await Clients.User(receiverId)
                    .SendAsync("Typing", sender.Name, sender.Id);
            }
        }

        public async Task StopTyping(string receiverId)
        {
            var senderId = Context.UserIdentifier;
            var sender = unit.User.GetByFilter(u => u.Id == senderId);

            if (sender != null)
            {
                await Clients.User(receiverId)
                    .SendAsync("StopTyping", sender.Name, sender.Id);
            }
        }

        public async Task GetOnlineUsers()
        {
            await Clients.Caller.SendAsync("OnlineUsersList", Connections.Keys.ToList());
        }


        public async Task JoinGroup(int groupId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
                return;

            var ismemeber = unit.GroupMember.GetALL(g => g.GroupId == groupId && g.UserId == userId).Any();
            if (!ismemeber)
            {
                return;
            }

            var groupName = "Group" + groupId;
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            if (!UserGroups.ContainsKey(userId))
            {
                UserGroups[userId] = new HashSet<int>();
            }
            UserGroups[userId].Add(groupId);

        }


        public async Task LeaveGroup(int groupId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
                return;

            var groupName = "Group" + groupId;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            if (UserGroups.ContainsKey(userId))
            {
                UserGroups[userId].Remove(groupId);
            }

            if (GroupTypingUsers.ContainsKey(groupId))
            {
                GroupTypingUsers[groupId].Remove(userId);
            }

          
        }


        public async Task StartTyping(int groupId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
                return;
            var ismemeber = unit.GroupMember.GetALL(g => g.GroupId == groupId && g.UserId == userId).Any();
            if (!ismemeber)
            {
                return;
            }

            if (!GroupTypingUsers.ContainsKey(groupId))
            {
                GroupTypingUsers[groupId] = new HashSet<string>();
            }

            GroupTypingUsers[groupId].Add(userId);
            var groupName = "Group" + groupId;

            var user = unit.User.GetByFilter(u => u.Id == userId);
            await Clients.OthersInGroup(groupName).SendAsync("UserStartedTyping", new
            {
                groupId = groupId,
                userId = userId,
                userName = user?.Name,
            });

        }

        public async Task GroupStopTyping(int groupId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
                return;

            if (GroupTypingUsers.ContainsKey(groupId))
            {
                GroupTypingUsers[groupId].Remove(userId);
            }

            var user = unit.User.GetByFilter(u => u.Id == userId);
            var groupName = "Group" + groupId;

            await Clients.OthersInGroup(groupName).SendAsync("UserStoppedTyping", new
            {
                groupId = groupId,
                userId = userId,
                userName = user?.Name
            });
        }



        public async Task<List<string>> GetTypingUsers(int groupId)
        {
            if (GroupTypingUsers.ContainsKey(groupId))
            {
                return GroupTypingUsers[groupId].ToList();
            }
            return new List<string>();
        }
    }
}
