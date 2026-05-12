using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NEEFRA.Core.DTO.Service;
using NEEFRA.Core.Services;
using Realtima_Chat_project.DataAccess.Reposatory.IReposatory;
using Realtima_Chat_project.DTO;
using Realtima_Chat_project.Models;
using Restaurant.Core.DTO.Group;
using Restaurant.Core.Entity.Chat;
using Restaurant.Core.Models.Account;
using Restaurant.Infrastructure.Reposatory;
using SignalIR_practice.Hubs;
using System.Linq.Expressions;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;
using Xunit;

namespace LiveTalkTests.UnitTesting.ServiceUnitTesting
{
    public class GroupServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitMock = new();
        private readonly Mock<IHubContext<ChatHub>> _hubContextMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IWebHostEnvironment> _envMock = new();
        private readonly Mock<ILogger<GroupService>> _loggerMock = new();

        private readonly Mock<IGroupReposatory> _groupRepoMock = new();
        private readonly Mock<IGroupMemberReposatory> _memberRepoMock = new();
        private readonly Mock<IGroupMessagesReposatory> _groupMsgRepoMock = new();
        private readonly Mock<IGroupMessageDeletedRepo> _msgDeletedRepoMock = new();

        private readonly Mock<IHubClients> _hubClientsMock = new();
        private readonly Mock<IGroupManager> _groupManagerMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();

        public GroupServiceTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            _hubClientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
            _hubContextMock.Setup(h => h.Groups).Returns(_groupManagerMock.Object);
            _clientProxyMock
                .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);
        }

        private GroupService CreateSut()
        {
            _unitMock.Setup(u => u.Group).Returns(_groupRepoMock.Object);
            _unitMock.Setup(u => u.GroupMember).Returns(_memberRepoMock.Object);
            _unitMock.Setup(u => u.GroupMessages).Returns(_groupMsgRepoMock.Object);
            _unitMock.Setup(u => u.GroupMessageDeleted).Returns(_msgDeletedRepoMock.Object);
            _unitMock.Setup(u => u.save());

            return new GroupService(
                _unitMock.Object,
                _hubContextMock.Object,
                _userManagerMock.Object,
                _envMock.Object,
                _loggerMock.Object);
        }


        [Fact]
        public async Task CreateGroup_UserFound_CreatesGroupAndMember()
        {
            // Arrange
            const string userId = "creator-1";
            var dto = new CreateGroupDTO { Name = "Test Group", Description = "Desc" };
            var user = new ApplicationUser { Id = userId, Name = "Creator" };

            var createdGroup = new Group
            {
                Id = 1,
                Name = dto.Name,
                CreatorId = userId,
                JoinCode = "ABC123",
                IsActive = true
            };

            _groupRepoMock
                .SetupSequence(r => r.GetByFilter(
                    It.IsAny<Expression<Func<Group, bool>>>(),
                    It.IsAny<string>()))
                .Returns((Group?)null) 
                .Returns(createdGroup); 

            _groupRepoMock.Setup(r => r.Create(It.IsAny<Group>()));
            _memberRepoMock.Setup(r => r.Create(It.IsAny<GroupMember>()));

            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

            var sut = CreateSut();

            // Act
            var result = await sut.CreateGroupAsync(userId, dto, "https://example.com");

            // Assert
            Assert.True(result.IsSuccess);
            _groupRepoMock.Verify(r => r.Create(It.Is<Group>(g =>
                g.Name == dto.Name &&
                g.CreatorId == userId &&
                g.IsActive)), Times.Once);
            _memberRepoMock.Verify(r => r.Create(It.Is<GroupMember>(m =>
                m.UserId == userId && m.IsAdmin)), Times.Once);
        }

        [Fact]
        public async Task CreateGroup_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            _groupRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Group, bool>>>(),
            It.IsAny<string>()))
                .Returns((Group?)null);

            _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var sut = CreateSut();
            var result = await sut.CreateGroupAsync("ghost", new CreateGroupDTO { Name = "X" }, "https://example.com");

            Assert.False(result.IsSuccess);
            Assert.Equal("NotFound", result.ErrorType);
        }


        [Fact]
        public async Task JoinGroup_InvalidCode_ReturnsNotFound()
        {
            _groupRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Group, bool>>>(),
            It.IsAny<string>()))
                .Returns((Group?)null);

            var sut = CreateSut();
            var result = await sut.JoinGroupAsync("user-1",
                new JoinGroupDTO { JoinCode = "INVALID" }, "https://example.com");

            Assert.False(result.IsSuccess);
            Assert.Equal("NotFound", result.ErrorType);
        }

        [Fact]
        public async Task JoinGroup_AlreadyMember_ReturnsBadRequest()
        {
            // Arrange
            var group = new Group { Id = 10, JoinCode = "ABCD1234", Name = "G" };
            _groupRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Group, bool>>>(),
            It.IsAny<string>()))
                .Returns(group);

            _memberRepoMock
                .Setup(r => r.GetALL(It.Is<Expression<Func<GroupMember, bool>>>(
                    e => true),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<GroupMember>
                {
                    new() { GroupId = group.Id, UserId = "user-1", IsActive = true }
                });

            var sut = CreateSut();
            var result = await sut.JoinGroupAsync("user-1",
                new JoinGroupDTO { JoinCode = "ABCD1234" }, "https://example.com");

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
        }

        [Fact]
        public async Task JoinGroup_NewMember_CreatesRecordAndNotifies()
        {
            // Arrange
            var group = new Group { Id = 10, JoinCode = "ABCD1234", Name = "G" };
            var user = new ApplicationUser { Id = "user-new", Name = "New User" };

            _groupRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Group, bool>>>(),
            It.IsAny<string>()))
                .Returns(group);

            _memberRepoMock
                .Setup(r => r.GetALL(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<GroupMember>());

            _memberRepoMock.Setup(r => r.Create(It.IsAny<GroupMember>()));
            _userManagerMock.Setup(m => m.FindByIdAsync("user-new")).ReturnsAsync(user);

            var sut = CreateSut();
            var result = await sut.JoinGroupAsync("user-new",
                new JoinGroupDTO { JoinCode = "ABCD1234" }, "https://example.com");

            Assert.True(result.IsSuccess);
            _memberRepoMock.Verify(r => r.Create(It.Is<GroupMember>(m =>
                m.UserId == "user-new" &&
                m.GroupId == group.Id &&
                m.IsAdmin == false &&
                m.IsActive)), Times.Once);
        }


        [Fact]
        public async Task AddMembers_NonAdmin_ReturnsForbidden()
        {
            _memberRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>()))
                .Returns(new GroupMember { UserId = "user-1", GroupId = 1, IsAdmin = false, IsActive = true });

            var sut = CreateSut();
            var result = await sut.AddMembersAsync("user-1",
                new AddMembersDTO { GroupId = 1, UserIds = new List<string> { "user-2" } },
                "https://example.com");

            Assert.False(result.IsSuccess);
            Assert.Equal("Forbidden", result.ErrorType);
        }

        [Fact]
        public async Task AddMembers_Admin_AddsNewMembers()
        {
            const int groupId = 5;
            var group = new Group { Id = groupId, Name = "Dev Group" };

            _memberRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>()))
                .Returns(new GroupMember { UserId = "admin", GroupId = groupId, IsAdmin = true, IsActive = true });

            _groupRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Group, bool>>>(),
            It.IsAny<string>()))
                .Returns(group);

            _memberRepoMock
                .Setup(r => r.GetALL(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<GroupMember>());

            _memberRepoMock.Setup(r => r.Create(It.IsAny<GroupMember>()));

            _userManagerMock
                .Setup(m => m.FindByIdAsync("new-user"))
                .ReturnsAsync(new ApplicationUser { Id = "new-user", Name = "New User" });

            var sut = CreateSut();
            var result = await sut.AddMembersAsync("admin",
                new AddMembersDTO { GroupId = groupId, UserIds = new List<string> { "new-user" } },
                "https://example.com");

            Assert.True(result.IsSuccess);
            _memberRepoMock.Verify(r => r.Create(It.Is<GroupMember>(m =>
                m.UserId == "new-user" && m.GroupId == groupId)), Times.Once);
        }


        [Fact]
        public async Task SendGroupMessage_NotMember_ReturnsBadRequest()
        {
            _memberRepoMock
                .Setup(r => r.GetALL(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<GroupMember>());

            var sut = CreateSut();
            var result = await sut.SendMessageAsync("outsider",
                new SendGroupMessageDTO { GroupId = 1, Type = MessageType.Text, Text = "Hi" },
                "https://example.com");

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
        }

        [Fact]
        public async Task SendGroupMessage_ValidMember_CreatesMessageAndBroadcasts()
        {
            // Arrange: user is a member
            _memberRepoMock
                .Setup(r => r.GetALL(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<GroupMember>
                {
                    new() { GroupId = 1, UserId = "member", IsActive = true }
                });

            _groupMsgRepoMock.Setup(r => r.Create(It.IsAny<GroupMessage>()));

            var sut = CreateSut();
            var result = await sut.SendMessageAsync("member",
                new SendGroupMessageDTO { GroupId = 1, Type = MessageType.Text, Text = "Hello group!" },
                "https://example.com");

            Assert.True(result.IsSuccess);
            _groupMsgRepoMock.Verify(r => r.Create(It.IsAny<GroupMessage>()), Times.Once);
            _unitMock.Verify(u => u.save(), Times.AtLeastOnce);
        }


        [Fact]
        public async Task LeaveGroup_NotMember_ReturnsBadRequest()
        {
            _memberRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>()))
                .Returns((GroupMember?)null);

            var sut = CreateSut();
            var result = await sut.LeaveGroupAsync("user-1", 1);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task LeaveGroup_ValidMember_DeactivatesMembership()
        {
            // Arrange
            var member = new GroupMember { GroupId = 1, UserId = "user-1", IsActive = true, IsAdmin = false };

            _memberRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>()))
                .Returns(member);

            // No other members (simple test)
            _memberRepoMock
                .Setup(r => r.GetALL(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<GroupMember>());

            _memberRepoMock.Setup(r => r.Update(member));

            var sut = CreateSut();
            var result = await sut.LeaveGroupAsync("user-1", 1);

            Assert.True(result.IsSuccess);
            Assert.False(member.IsActive);
        }


        [Fact]
        public async Task RemoveMember_NonAdmin_ReturnsForbidden()
        {
            _memberRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>()))
                .Returns(new GroupMember { UserId = "caller", GroupId = 1, IsAdmin = false, IsActive = true });

            var sut = CreateSut();
            var result = await sut.RemoveMemberAsync("caller",
                new RemoveMemberDTO { GroupId = 1, UserId = "target" });

            Assert.False(result.IsSuccess);
            Assert.Equal("Forbidden", result.ErrorType);
        }

        [Fact]
        public async Task RemoveMember_AdminRemovesTarget_DeactivatesMembership()
        {
            // Arrange
            var adminMember = new GroupMember { UserId = "admin", GroupId = 2, IsAdmin = true, IsActive = true };
            var targetMember = new GroupMember { UserId = "target", GroupId = 2, IsAdmin = false, IsActive = true };

            // First call returns admin (the caller), second returns target
            var callCount = 0;
            _memberRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>()))
                .Returns(() => callCount++ == 0 ? adminMember : targetMember);

            _memberRepoMock.Setup(r => r.Update(targetMember));

            var sut = CreateSut();
            var result = await sut.RemoveMemberAsync("admin",
                new RemoveMemberDTO { GroupId = 2, UserId = "target" });

            Assert.True(result.IsSuccess);
            Assert.False(targetMember.IsActive);
        }


        [Fact]
        public async Task GetUnreadCount_NotMember_ReturnsForbidden()
        {
            _memberRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>()))
                .Returns((GroupMember?)null);

            var sut = CreateSut();
            var result = await sut.GetUnreadCountAsync("user-1", 1);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task GetUnreadCount_ValidMember_ReturnsCount()
        {
            _memberRepoMock
                .Setup(r => r.GetALL(
                    It.IsAny<Expression<Func<GroupMember, bool>>>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .Returns(new List<GroupMember>
                {
            new GroupMember
            {
                UserId = "user-1",
                GroupId = 1,
                IsActive = true,
                JoinedAt = DateTime.UtcNow.AddDays(-1)
            }
                });

            _memberRepoMock
                .Setup(r => r.GetByFilter(
                    It.IsAny<Expression<Func<GroupMember, bool>>>(),
                    It.IsAny<string>()))
                .Returns(new GroupMember
                {
                    UserId = "user-1",
                    GroupId = 1,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddDays(-1)
                });

            _groupMsgRepoMock
                .Setup(r => r.GetALL(
                    It.IsAny<Expression<Func<GroupMessage, bool>>>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .Returns(new List<GroupMessage>
                {
            new() { Id = 1, GroupId = 1 },
            new() { Id = 2, GroupId = 1 }
                });

            var readByRepoMock = new Mock<IGroupMessageReadRepo>();

            readByRepoMock
                .Setup(r => r.GetALL(
                    It.IsAny<Expression<Func<GroupMessageRead, bool>>>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .Returns(new List<GroupMessageRead>());

            _unitMock.Setup(u => u.GroupMessageRead)
                .Returns(readByRepoMock.Object);

            var sut = CreateSut();

            var result = await sut.GetUnreadCountAsync("user-1", 1);

            Assert.True(result.IsSuccess);
            Assert.True(result.Data >= 0);
        }


        [Fact]
        public async Task DeleteGroupMessageForMe_MessageNotFound_ReturnsNotFound()
        {
            _groupMsgRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<GroupMessage, bool>>>(),
            It.IsAny<string>()))
                .Returns((GroupMessage?)null);

            var sut = CreateSut();
            var result = await sut.DeleteMessageForMeAsync("user-1", 999);

            Assert.False(result.IsSuccess);
            Assert.Equal("NotFound", result.ErrorType);
        }

        [Fact]
        public async Task DeleteGroupMessageForMe_Valid_CreatesDeleteRecord()
        {
        
            var msg = new GroupMessage
            {
                Id = 20,
                GroupId = 1,
                SenderId = "other"
            };

            _groupMsgRepoMock
                .Setup(r => r.GetByFilter(
                    It.IsAny<Expression<Func<GroupMessage, bool>>>(),
                    It.IsAny<string>()))
                .Returns(msg);

            _memberRepoMock
                .Setup(r => r.GetALL(
                    It.IsAny<Expression<Func<GroupMember, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<GroupMember>
                {
            new GroupMember
            {
                GroupId = 1,
                UserId = "user-1",
                IsActive = true
            }
                });

            _msgDeletedRepoMock
                .Setup(r => r.GetByFilter(
                    It.IsAny<Expression<Func<GroupMessageDeleted, bool>>>(),
                    It.IsAny<string>()))
                .Returns((GroupMessageDeleted?)null);

            _msgDeletedRepoMock
                .Setup(r => r.Create(It.IsAny<GroupMessageDeleted>()));

            var sut = CreateSut();

            // Act
            var result = await sut.DeleteMessageForMeAsync("user-1", 20);

            // Assert
            Assert.True(result.IsSuccess);

            _msgDeletedRepoMock.Verify(r =>
                r.Create(It.Is<GroupMessageDeleted>(d =>
                    d.MessageId == 20 &&
                    d.UserId == "user-1" &&
                    d.DeletedForEveryone == false)),
                Times.Once);
        }
    }
}
