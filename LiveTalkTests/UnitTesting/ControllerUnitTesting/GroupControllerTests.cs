namespace LiveTalkTests.UnitTesting.ControllerUnitTesting
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Moq;
    using NEEFRA.Core;
    using NEEFRA.Core.DTO.Service;
    using Realtima_Chat_project.Controllers;
    using Realtima_Chat_project.DTO;
    using Restaurant.Core.DTO.Group;
    using System.Security.Claims;
    using Xunit;

    public class GroupControllerTests
    {

        private readonly Mock<IGroupService> _serviceMock = new();

        private GroupController CreateSut(string userId = "user-1")
        {
            var controller = new GroupController(_serviceMock.Object);
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var httpCtx = new DefaultHttpContext { User = principal };
            httpCtx.Request.Scheme = "https";
            httpCtx.Request.Host = new HostString("example.com");
            controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };
            return controller;
        }


        [Fact]
        public async Task CreateGroup_ValidDto_Returns200()
        {
            // Arrange
            var dto = new CreateGroupDTO { Name = "Devs", Description = "Dev group" };
            _serviceMock
                .Setup(s => s.CreateGroupAsync("user-1", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok(new { Id = 1, Name = "Devs" }, "Group created"));

            // Act
            var result = await CreateSut("user-1").CreateGroup(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task CreateGroup_UserNotFound_Returns404()
        {
            // Arrange
            var dto = new CreateGroupDTO { Name = "X" };
            _serviceMock
                .Setup(s => s.CreateGroupAsync("ghost", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("User not found", "NotFound"));

            // Act
            var result = await CreateSut("ghost").CreateGroup(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }


        [Fact]
        public async Task JoinGroup_InvalidCode_Returns404()
        {
            // Arrange
            var dto = new JoinGroupDTO { JoinCode = "INVALID" };
            _serviceMock
                .Setup(s => s.JoinGroupAsync("user-1", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Group not found", "NotFound"));

            // Act
            var result = await CreateSut("user-1").JoinGroup(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }

        [Fact]
        public async Task JoinGroup_AlreadyMember_Returns400()
        {
            // Arrange
            var dto = new JoinGroupDTO { JoinCode = "ABCD1234" };
            _serviceMock
                .Setup(s => s.JoinGroupAsync("user-1", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Already a member", "BadRequest"));

            // Act
            var result = await CreateSut("user-1").JoinGroup(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }

        [Fact]
        public async Task JoinGroup_NewMember_Returns200()
        {
            // Arrange
            var dto = new JoinGroupDTO { JoinCode = "ABCD1234" };
            _serviceMock
                .Setup(s => s.JoinGroupAsync("user-1", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok(new { }, "Joined successfully"));

            // Act
            var result = await CreateSut("user-1").JoinGroup(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task AddMembers_NonAdmin_Returns403()
        {
            // Arrange
            var dto = new AddMembersDTO { GroupId = 1, UserIds = new List<string> { "user-2" } };
            _serviceMock
                .Setup(s => s.AddMembersAsync("user-1", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Not admin", "Forbidden"));

            // Act
            var raw = await CreateSut("user-1").AddMembers(dto);
            Assert.IsType<ForbidResult>(raw);
        }

        [Fact]
        public async Task AddMembers_Admin_Returns200()
        {
            // Arrange
            var dto = new AddMembersDTO { GroupId = 1, UserIds = new List<string> { "user-2" } };
            _serviceMock
                .Setup(s => s.AddMembersAsync("admin", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok(new { }, "Members added"));

            // Act
            var result = await CreateSut("admin").AddMembers(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task RemoveMember_NonAdmin_Returns403()
        {
            // Arrange
            var dto = new RemoveMemberDTO { GroupId = 1, UserId = "user-2" };
            _serviceMock
                .Setup(s => s.RemoveMemberAsync("user-1", dto))
                .ReturnsAsync(ServiceResult<object>.Fail("Not admin", "Forbidden"));

            // Act
            var raw = await CreateSut("user-1").RemoveMember(dto);
            Assert.IsType<ForbidResult>(raw);
        }

        [Fact]
        public async Task RemoveMember_Admin_Returns200()
        {
            // Arrange
            var dto = new RemoveMemberDTO { GroupId = 1, UserId = "target" };
            _serviceMock
                .Setup(s => s.RemoveMemberAsync("admin", dto))
                .ReturnsAsync(ServiceResult<object>.Ok(new { }, "Member removed"));

            // Act
            var result = await CreateSut("admin").RemoveMember(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task LeaveGroup_NotMember_Returns400()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.LeaveGroupAsync("user-1", 1))
                .ReturnsAsync(ServiceResult<object>.Fail("Not a member", "BadRequest"));

            // Act
            var result = await CreateSut("user-1").LeaveGroup(1) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }

        [Fact]
        public async Task LeaveGroup_ValidMember_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.LeaveGroupAsync("user-1", 1))
                .ReturnsAsync(ServiceResult<object>.Ok(new { }, "Left group"));

            // Act
            var result = await CreateSut("user-1").LeaveGroup(1) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task UpdateGroup_NonAdmin_Returns403()
        {
            // Arrange
            var dto = new UpdateGroupDTO { Name = "New Name" };
            _serviceMock
                .Setup(s => s.UpdateGroupAsync("user-1", 1, dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Not admin", "Forbidden"));

            // Act
            var raw = await CreateSut("user-1").UpdateGroup(1, dto);
            Assert.IsType<ForbidResult>(raw);
        }

        [Fact]
        public async Task UpdateGroup_Admin_Returns200()
        {
            // Arrange
            var dto = new UpdateGroupDTO { Name = "New Name" };
            _serviceMock
                .Setup(s => s.UpdateGroupAsync("admin", 1, dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok(new { }, "Group updated"));

            // Act
            var result = await CreateSut("admin").UpdateGroup(1, dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task GetGroupById_Exists_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetGroupByIdAsync("user-1", 5, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok(new { Id = 5, Name = "Devs" }));

            // Act
            var result = await CreateSut("user-1").GetGroupById(5) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetGroupById_NotFound_Returns404()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetGroupByIdAsync("user-1", 999, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Group not found", "NotFound"));

            // Act
            var result = await CreateSut("user-1").GetGroupById(999) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }


        [Fact]
        public async Task SendGroupMessage_NotMember_Returns400()
        {
            // Arrange
            var dto = new SendGroupMessageDTO { GroupId = 1, Type = Realtima_Chat_project.Models.MessageType.Text, Text = "Hi" };
            _serviceMock
                .Setup(s => s.SendMessageAsync("outsider", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Not a member", "BadRequest"));

            // Act
            var result = await CreateSut("outsider").SendMessage(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }

        [Fact]
        public async Task SendGroupMessage_ValidMember_Returns200()
        {
            // Arrange
            var dto = new SendGroupMessageDTO { GroupId = 1, Type = Realtima_Chat_project.Models.MessageType.Text, Text = "Hello!" };
            _serviceMock
                .Setup(s => s.SendMessageAsync("user-1", dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok(new { }, "Message sent"));

            // Act
            var result = await CreateSut("user-1").SendMessage(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task DeleteGroupMessageForMe_Valid_Returns200()
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
        public async Task DeleteGroupMessageForEveryone_NotSender_Returns403()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteMessageForEveryoneAsync("user-1", 5))
                .ReturnsAsync(ServiceResult<object>.Fail("Not sender", "Forbidden"));

            // Act
            var raw = await CreateSut("user-1").DeleteMessageForEveryone(5);
            Assert.IsType<ForbidResult>(raw);
        }

        [Fact]
        public async Task ClearGroupChat_ValidMember_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.ClearChatAsync("user-1", 1))
                .ReturnsAsync(ServiceResult<object>.Ok("Chat cleared"));

            // Act
            var result = await CreateSut("user-1").ClearChatForMe(1) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task MarkGroupMessageAsRead_Valid_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.MarkAsReadAsync("user-1", 3))
                .ReturnsAsync(ServiceResult<object>.Ok("Marked"));

            // Act
            var result = await CreateSut("user-1").MarkAsRead(3) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task WhoRead_NotMemberOrSender_Returns403()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.WhoReadAsync("user-1", 7, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Fail("Forbidden", "Forbidden"));

            // Act
            var raw = await CreateSut("user-1").WhoReadMessage(7);
            Assert.IsType<ForbidResult>(raw);
        }

        [Fact]
        public async Task GetGroupUnreadCount_NotMember_Returns400()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetUnreadCountAsync("user-1", 1))
                .ReturnsAsync(ServiceResult<int>.Fail("Not a member", "BadRequest"));

            // Act
            var result = await CreateSut("user-1").GetUnreadCount(1) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }

        [Fact]
        public async Task GetGroupUnreadCount_ValidMember_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetUnreadCountAsync("user-1", 1))
                .ReturnsAsync(ServiceResult<int>.Ok(3));

            // Act
            var result = await CreateSut("user-1").GetUnreadCount(1) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }
    }
}