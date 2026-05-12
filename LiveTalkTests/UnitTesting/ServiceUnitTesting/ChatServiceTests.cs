using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NEEFRA.Core;
using NEEFRA.Core.DTO.Service;
using NEEFRA.Core.Services;
using Realtima_Chat_project.DataAccess.Reposatory.IReposatory;
using Realtima_Chat_project.DTO;
using Realtima_Chat_project.Hubs;
using Realtima_Chat_project.Models;
using Restaurant.Core.DTO.Chat;
using Restaurant.Core.Entity.Chat;
using Restaurant.Core.Models.Account;
using SignalIR_practice.Hubs;
using System.Linq.Expressions;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;
using Xunit;

namespace LiveTalkTests.UnitTesting.ServiceUnitTesting
{
    public class ChatServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitMock = new();
        private readonly Mock<IHubContext<ChatHub>> _hubContextMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IWebHostEnvironment> _envMock = new();
        private readonly Mock<ILogger<ChatService>> _loggerMock = new();

        private readonly Mock<IMessageReposatory> _messageRepoMock = new();
        private readonly Mock<IBlockprepo> _blockRepoMock = new();
        private readonly Mock<IMessageDeletedRepo> _deletedRepoMock = new();

        private readonly Mock<IHubClients> _hubClientsMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();

        public ChatServiceTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _hubClientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
            _clientProxyMock
                .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);
        }

        private ChatService CreateSut()
        {
            _unitMock.Setup(u => u.Message).Returns(_messageRepoMock.Object);
            _unitMock.Setup(u => u.BlockedUsers).Returns(_blockRepoMock.Object);
            _unitMock.Setup(u => u.MessageDeleted).Returns(_deletedRepoMock.Object);
            _unitMock.Setup(u => u.save());

            return new ChatService(
                _unitMock.Object,
                _hubContextMock.Object,
                _userManagerMock.Object,
                _envMock.Object,
                _loggerMock.Object);
        }


        private void SetupNotBlocked(string userId1, string userId2)
        {
            _blockRepoMock
                .Setup(r => r.GetALL(
            It.IsAny<Expression<Func<BlockedUser, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<BlockedUser>());
        }

        private void SetupBlocked()
        {
            _blockRepoMock
                .Setup(r => r.GetALL(
            It.IsAny<Expression<Func<BlockedUser, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<BlockedUser>
                {
                    new() { BlockerId = "user-1", BlockedUserId = "user-2", IsActive = true }
                });
        }


        [Fact]
        public async Task SendMessage_NotBlocked_CreatesMessageAndBroadcasts()
        {
            // Arrange
            SetupNotBlocked("sender", "receiver");
            _messageRepoMock.Setup(r => r.Create(It.IsAny<Message>()));

            var sut = CreateSut();

            var dto = new MessageDTO
            {
                ReceiverId = "receiver",
                Type = MessageType.Text,
                Text = "Hello!"
            };

            // Act
            var result = await sut.SendMessageAsync("sender", dto, "https://example.com");

            // Assert
            Assert.True(result.IsSuccess);
            _messageRepoMock.Verify(r => r.Create(It.IsAny<Message>()), Times.Once);
            _unitMock.Verify(u => u.save(), Times.Once);
            // Both sender and receiver notified
            _hubClientsMock.Verify(c => c.User("receiver"), Times.AtLeastOnce);
            _hubClientsMock.Verify(c => c.User("sender"), Times.AtLeastOnce);
        }

        [Fact]
        public async Task SendMessage_Blocked_ReturnsBadRequest()
        {
            // Arrange
            SetupBlocked();
            var sut = CreateSut();

            // Act
            var result = await sut.SendMessageAsync("user-1",
                new MessageDTO { ReceiverId = "user-2", Type = MessageType.Text, Text = "Hi" },
                "https://example.com");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
            _messageRepoMock.Verify(r => r.Create(It.IsAny<Message>()), Times.Never);
        }


        [Fact]
        public async Task ReplyToMessage_OriginalNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupNotBlocked("sender", "receiver");
            _messageRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Message, bool>>>(),
            It.IsAny<string>()))
                .Returns((Message?)null);

            var sut = CreateSut();

            // Act
            var result = await sut.ReplyToMessageAsync("sender",
                new ReplyToMessageDTOFor_OneToOne
                {
                    ReceiverId = "receiver",
                    Type = MessageType.Text,
                    Text = "Reply",
                    ReplyToMessageId = 999
                },
                "https://example.com");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("NotFound", result.ErrorType);
        }

        [Fact]
        public async Task ReplyToMessage_ValidReply_CreatesMessageWithReplyRef()
        {
            // Arrange
            SetupNotBlocked("sender", "receiver");
            var original = new Message { Id = 5, TextContent = "Original text", Type = MessageType.Text };

            _messageRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Message, bool>>>(),
            It.IsAny<string>()))
                .Returns(original);

            _messageRepoMock.Setup(r => r.Create(It.IsAny<Message>()));

            var sut = CreateSut();

            // Act
            var result = await sut.ReplyToMessageAsync("sender",
                new ReplyToMessageDTOFor_OneToOne
                {
                    ReceiverId = "receiver",
                    Type = MessageType.Text,
                    Text = "Reply",
                    ReplyToMessageId = 5
                },
                "https://example.com");

            // Assert
            Assert.True(result.IsSuccess);
            _messageRepoMock.Verify(r => r.Create(It.Is<Message>(m => m.ReplyToMessageId == 5)), Times.Once);
        }


        [Fact]
        public async Task MarkAsDelivered_MessageNotFound_ReturnsNotFound()
        {
            _messageRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Message, bool>>>(),
            It.IsAny<string>()))
                .Returns((Message?)null);

            var sut = CreateSut();
            var result = await sut.MarkAsDeliveredAsync("user-1", 404);

            Assert.False(result.IsSuccess);
            Assert.Equal("NotFound", result.ErrorType);
        }

        [Fact]
        public async Task MarkAsDelivered_AlreadyDelivered_DoesNotSaveOrNotify()
        {
            var message = new Message
            {
                Id = 1,
                SenderId = "sender",
                ReceiverId = "receiver",
                DeliveredAt = DateTime.UtcNow.AddMinutes(-5)  
            };

            _messageRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Message, bool>>>(),
            It.IsAny<string>()))
                .Returns(message);

            var sut = CreateSut();
            var result = await sut.MarkAsDeliveredAsync("receiver", 1);

            Assert.True(result.IsSuccess);
            _unitMock.Verify(u => u.save(), Times.Never);
        }

        [Fact]
        public async Task MarkAsDelivered_NotYetDelivered_SetsTimestampAndNotifies()
        {
            var message = new Message
            {
                Id = 2,
                SenderId = "sender",
                ReceiverId = "receiver",
                DeliveredAt = null
            };

            _messageRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Message, bool>>>(),
            It.IsAny<string>()))
                .Returns(message);

            var sut = CreateSut();
            var result = await sut.MarkAsDeliveredAsync("receiver", 2);

            Assert.True(result.IsSuccess);
            Assert.NotNull(message.DeliveredAt);
            _unitMock.Verify(u => u.save(), Times.Once);
        }


        [Fact]
        public async Task MarkAsRead_MultipleUnread_MarksAllAndNotifies()
        {
            // Arrange
            var messages = new List<Message>
            {
                new() { Id = 10, SenderId = "sender", ReceiverId = "receiver", ReadAt = null },
                new() { Id = 11, SenderId = "sender", ReceiverId = "receiver", ReadAt = null }
            };

            _messageRepoMock
                .Setup(r => r.GetALL(
            It.IsAny<Expression<Func<Message, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(messages);

            var sut = CreateSut();
            var result = await sut.MarkAsReadAsync("receiver", "sender");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.All(messages, m => Assert.NotNull(m.ReadAt));
            _hubClientsMock.Verify(c => c.User("sender"), Times.Once);
        }


        [Fact]
        public async Task GetUnreadCount_Returns_CorrectCount()
        {
            _messageRepoMock
                .Setup(r => r.GetALL(
            It.IsAny<Expression<Func<Message, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<Message>
                {
                    new() { Id = 1, ReadAt = null },
                    new() { Id = 2, ReadAt = null },
                    new() { Id = 3, ReadAt = null }
                });

            var sut = CreateSut();
            var result = await sut.GetUnreadCountAsync("receiver", "sender");

            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Data);
        }


        [Fact]
        public async Task DeleteMessageForMe_MessageNotFound_ReturnsNotFound()
        {
            _messageRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Message, bool>>>(),
            It.IsAny<string>()))
                .Returns((Message?)null);

            var sut = CreateSut();
            var result = await sut.DeleteMessageForMeAsync("user-1", 0);

            Assert.False(result.IsSuccess);
            Assert.Equal("NotFound", result.ErrorType);
        }

        [Fact]
        public async Task DeleteMessageForMe_AlreadyDeleted_ReturnsBadRequest()
        {
            _messageRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Message, bool>>>(),
            It.IsAny<string>()))
                .Returns(new Message { Id = 5 });

            _deletedRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<MessageDeleted, bool>>>(),
            It.IsAny<string>()))
                .Returns(new MessageDeleted { MessageId = 5, UserId = "user-1" });

            var sut = CreateSut();
            var result = await sut.DeleteMessageForMeAsync("user-1", 5);

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
        }

        [Fact]
        public async Task DeleteMessageForMe_Valid_CreatesDeletedRecord()
        {
            _messageRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Message, bool>>>(),
            It.IsAny<string>()))
                .Returns(new Message { Id = 7 });

            _deletedRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<MessageDeleted, bool>>>(),
            It.IsAny<string>()))
                .Returns((MessageDeleted?)null);

            _deletedRepoMock.Setup(r => r.Create(It.IsAny<MessageDeleted>()));

            var sut = CreateSut();
            var result = await sut.DeleteMessageForMeAsync("user-1", 7);

            Assert.True(result.IsSuccess);
            _deletedRepoMock.Verify(r => r.Create(It.Is<MessageDeleted>(d =>
                d.MessageId == 7 &&
                d.UserId == "user-1" &&
                d.DeletedForEveryone == false)), Times.Once);
            _unitMock.Verify(u => u.save(), Times.Once);
        }


        [Fact]
        public async Task GetMessagesWithUser_Blocked_ReturnsForbidden()
        {
            SetupBlocked();
            var sut = CreateSut();
            var result = await sut.GetMessagesWithUserAsync("user-1", "user-2", "https://example.com");

            Assert.False(result.IsSuccess);
            Assert.Equal("Forbidden", result.ErrorType);
        }

        [Fact]
        public async Task GetMessagesWithUser_NotBlocked_ReturnsMessages()
        {
            SetupNotBlocked("user-1", "user-2");

            _messageRepoMock
                .Setup(r => r.GetALL(
            It.IsAny<Expression<Func<Message, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<Message>
                {
                    new() { Id = 1, SenderId = "user-1", ReceiverId = "user-2",
                            Type = MessageType.Text, TextContent = "Hi", CreatedAt = DateTime.UtcNow }
                });

            _deletedRepoMock
                .Setup(r => r.GetALL(
            It.IsAny<Expression<Func<MessageDeleted, bool>>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<MessageDeleted>());

            var sut = CreateSut();
            var result = await sut.GetMessagesWithUserAsync("user-1", "user-2", "https://example.com");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data);
        }
    }
}
