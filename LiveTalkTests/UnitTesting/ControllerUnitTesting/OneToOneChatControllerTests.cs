namespace LiveTalkTests.UnitTesting.ControllerUnitTesting
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;
    using NEEFRA.Core;
    using NEEFRA.Core.DTO.Service;
    using Realtima_Chat_project.Controllers;
    using Realtima_Chat_project.DTO;
    using Restaurant.Core.DTO.Chat;
    using System.Security.Claims;
    using Xunit;

    public class OneToOneChatControllerTests
    {

        private readonly Mock<IChatService> _serviceMock = new();

        private OneToOneChatController CreateSut(string userId = "user-1")
        {
            var controller = new OneToOneChatController(_serviceMock.Object);
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var httpCtx = new DefaultHttpContext { User = principal };
            httpCtx.Request.Scheme = "https";
            httpCtx.Request.Host = new HostString("example.com");
            controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };
            return controller;
        }


        [Fact]
        public async Task SendMessage_NotBlocked_Returns200()
        {
            // Arrange
            var dto = new MessageDTO { ReceiverId = "user-2", Type = Realtima_Chat_project.Models.MessageType.Text, Text = "Hello!" };
            _serviceMock
                .Setup(s => s.SendMessageAsync("user-1", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok(dto, "Message sent"));

            // Act
            var result = await CreateSut("user-1").SendMessage(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task SendMessage_Blocked_Returns400()
        {
            // Arrange
            var dto = new MessageDTO { ReceiverId = "user-2", Type = Realtima_Chat_project.Models.MessageType.Text, Text = "Hi" };
            _serviceMock
                .Setup(s => s.SendMessageAsync("user-1", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("User is blocked", "BadRequest"));

            // Act
            var result = await CreateSut("user-1").SendMessage(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }


        [Fact]
        public async Task ReplyToMessage_OriginalNotFound_Returns404()
        {
            // Arrange
            var dto = new ReplyToMessageDTOFor_OneToOne { ReceiverId = "user-2", ReplyToMessageId = 999 };
            _serviceMock
                .Setup(s => s.ReplyToMessageAsync("user-1", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Original message not found", "NotFound"));

            // Act
            var result = await CreateSut("user-1").ReplyToMessage(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }

        [Fact]
        public async Task ReplyToMessage_Valid_Returns200()
        {
            // Arrange
            var dto = new ReplyToMessageDTOFor_OneToOne { ReceiverId = "user-2", ReplyToMessageId = 5 };
            var replyDto = new MessageDTO { ReceiverId = "user-2" };
            _serviceMock
                .Setup(s => s.ReplyToMessageAsync("user-1", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok(replyDto));

            // Act
            var result = await CreateSut("user-1").ReplyToMessage(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task GetUsers_Returns200WithUserList()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetUsersAsync("user-1", It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object> { new { Id = "user-2" } }));

            // Act
            var result = await CreateSut("user-1").GetUsers() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task GetMessagesWithUser_NotBlocked_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetMessagesWithUserAsync("user-1", "user-2", It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<MessageDTO>()));

            // Act
            var result = await CreateSut("user-1").GetMessagesWithUser("user-2") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetMessagesWithUser_Blocked_Returns403()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetMessagesWithUserAsync("user-1", "user-2", It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Fail("Blocked", "Forbidden"));

            // Act
            var raw = await CreateSut("user-1").GetMessagesWithUser("user-2");
            Assert.IsType<ForbidResult>(raw);
        }


        [Fact]
        public async Task MarkAsDelivered_MessageExists_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.MarkAsDeliveredAsync("user-1", 1))
                .ReturnsAsync(ServiceResult<object>.Ok("Marked as delivered"));

            // Act
            var result = await CreateSut("user-1").MarkAsDelivered(1) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task MarkAsDelivered_MessageNotFound_Returns404()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.MarkAsDeliveredAsync("user-1", 404))
                .ReturnsAsync(ServiceResult<object>.Fail("Message not found", "NotFound"));

            // Act
            var result = await CreateSut("user-1").MarkAsDelivered(404) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }


        [Fact]
        public async Task MarkAsRead_BulkBySender_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.MarkAsReadAsync("user-1", "sender-id"))
                .ReturnsAsync(ServiceResult<object>.Ok("Marked as read"));

            // Act
            var result = await CreateSut("user-1").MarkAsRead("sender-id") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task MarkSingleAsRead_ValidMessage_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.MarkSingleAsReadAsync("user-1", 7))
                .ReturnsAsync(ServiceResult<object>.Ok("Marked"));

            // Act
            var result = await CreateSut("user-1").MarkSingleAsRead(7) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task GetUnreadCount_Returns200WithCount()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetUnreadCountAsync("user-1", "sender-id"))
                .ReturnsAsync(ServiceResult<int>.Ok(5));

            // Act
            var result = await CreateSut("user-1").GetUnreadCount("sender-id") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task DeleteMessageForMe_Valid_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteMessageForMeAsync("user-1", 10))
                .ReturnsAsync(ServiceResult<object>.Ok("Deleted"));

            // Act
            var result = await CreateSut("user-1").DeleteMessageForMe(10) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task DeleteMessageForMe_AlreadyDeleted_Returns400()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteMessageForMeAsync("user-1", 10))
                .ReturnsAsync(ServiceResult<object>.Fail("Already deleted", "BadRequest"));

            // Act
            var result = await CreateSut("user-1").DeleteMessageForMe(10) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }

        [Fact]
        public async Task DeleteMessageForEveryone_NotSender_Returns403()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteMessageForEveryoneAsync("user-1", 5))
                .ReturnsAsync(ServiceResult<object>.Fail("Not the sender", "Forbidden"));

            // Act
            var raw = await CreateSut("user-1").DeleteMessageForEveryone(5);
            Assert.IsType<ForbidResult>(raw);
        }

        [Fact]
        public async Task DeleteMessageForEveryone_Sender_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteMessageForEveryoneAsync("user-1", 5))
                .ReturnsAsync(ServiceResult<object>.Ok("Deleted for everyone"));

            // Act
            var result = await CreateSut("user-1").DeleteMessageForEveryone(5) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task ClearChat_ValidUser_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.ClearChatAsync("user-1", "user-2"))
                .ReturnsAsync(ServiceResult<object>.Ok("Chat cleared"));

            // Act
            var result = await CreateSut("user-1").ClearChat("user-2") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task BlockUser_Valid_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.BlockUserAsync("user-1", "user-2"))
                .ReturnsAsync(ServiceResult<object>.Ok("User blocked"));

            // Act
            var result = await CreateSut("user-1").BlockUser("user-2") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task UnblockUser_Valid_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.UnblockUserAsync("user-1", "user-2"))
                .ReturnsAsync(ServiceResult<object>.Ok("User unblocked"));

            // Act
            var result = await CreateSut("user-1").UnblockUser("user-2") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetBlockedUsers_Returns200WithList()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetBlockedUsersAsync("user-1", It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object>()));

            // Act
            var result = await CreateSut("user-1").GetBlockedUsers() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task GetMyChatList_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetMyChatListAsync("user-1", It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<ChatListItemDTO>>.Ok(new List<ChatListItemDTO>()));

            // Act
            var result = await CreateSut("user-1").GetMyChatList() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }
    }
}