using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.TestHelpers;
using Moq;
using NEEFRA.Core.DTO.Service;
using Realtima_Chat_project.DTO;
using Restaurant.Core.DTO.Group;
using Xunit;

namespace IntegrationTests
{
    public class GroupControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public GroupControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

     

        [Fact]
        public async Task GetGroupById_Member_Returns200()
        {
            _factory.GroupServiceMock
                .Setup(s => s.GetGroupByIdAsync(It.IsAny<string>(), 1, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok(new { Id = 1, Name = "Dev Team" }));

            var response = await _client.GetAsync("/api/v1/Group/GetGroup/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Dev Team");
        }

        [Fact]
        public async Task GetGroupById_NotFound_Returns404()
        {
            _factory.GroupServiceMock
                .Setup(s => s.GetGroupByIdAsync(It.IsAny<string>(), 999, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Group not found", "NotFound"));

            var response = await _client.GetAsync("/api/v1/Group/GetGroup/999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

       
        [Fact]
        public async Task GetGroupById_Unauthenticated_Returns401()
        {
            var anonClient = _factory.CreateClient();
            var response = await anonClient.GetAsync("/api/v1/Group/GetGroup/1");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

     

        [Fact]
        public async Task GetMyGroups_Returns200WithGroupList()
        {
            _factory.GroupServiceMock
                .Setup(s => s.GetMyGroupsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<List<object>>.Ok(new List<object>
                {
                    new { Id = 1, Name = "Dev Team" },
                    new { Id = 2, Name = "Design Squad" }
                }));

            var response = await _client.GetAsync("/api/v1/Group/MyGroups");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Dev Team").And.Contain("Design Squad");
        }

        [Fact]
        public async Task GetMyGroups_NoGroups_Returns200WithEmptyList()
        {
            _factory.GroupServiceMock
                .Setup(s => s.GetMyGroupsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<List<object>>.Ok(new List<object>()));

            var response = await _client.GetAsync("/api/v1/Group/MyGroups");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

       
      
        [Fact]
        public async Task JoinGroup_InvalidInviteCode_Returns400()
        {
            _factory.GroupServiceMock
                .Setup(s => s.JoinGroupAsync(It.IsAny<string>(), It.IsAny<JoinGroupDTO>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Invalid invite code"));

            var response = await _client.PostAsJsonAsync("/api/v1/Group/Join",
                new { GroupId = 1, InviteCode = "WRONG" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task JoinGroup_AlreadyMember_Returns400()
        {
            _factory.GroupServiceMock
                .Setup(s => s.JoinGroupAsync(It.IsAny<string>(), It.IsAny<JoinGroupDTO>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("You are already a member of this group"));

            var response = await _client.PostAsJsonAsync("/api/v1/Group/Join",
                new { GroupId = 1, InviteCode = "VALID" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }



        [Fact]
        public async Task AddMembers_Admin_Returns200()
        {
            _factory.GroupServiceMock
                .Setup(s => s.AddMembersAsync(It.IsAny<string>(), It.IsAny<AddMembersDTO>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok("Members added"));

            var response = await _client.PostAsJsonAsync("/api/v1/Group/AddMembers",
                new { GroupId = 1, UserIds = new[] { "user-2", "user-3" } });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

       


        [Fact]
        public async Task RemoveMember_Admin_Returns200()
        {
            _factory.GroupServiceMock
                .Setup(s => s.RemoveMemberAsync(It.IsAny<string>(), It.IsAny<RemoveMemberDTO>()))
                .ReturnsAsync(ServiceResult<object>.Ok("Member removed"));

            var response = await _client.PostAsJsonAsync("/api/v1/Group/RemoveMember",
                new { GroupId = 1, UserId = "user-2" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }




        [Fact]
        public async Task LeaveGroup_Member_Returns200()
        {
            _factory.GroupServiceMock
                .Setup(s => s.LeaveGroupAsync(It.IsAny<string>(), 1))
                .ReturnsAsync(ServiceResult<object>.Ok("Left group"));

            var response = await _client.PostAsync("/api/v1/Group/Leave/1", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LeaveGroup_NotMember_Returns400()
        {
            _factory.GroupServiceMock
                .Setup(s => s.LeaveGroupAsync(It.IsAny<string>(), 99))
                .ReturnsAsync(ServiceResult<object>.Fail("You are not a member of this group"));

            var response = await _client.PostAsync("/api/v1/Group/Leave/99", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }



        [Fact]
        public async Task GetGroupMembers_Member_Returns200WithList()
        {
            _factory.GroupServiceMock
                .Setup(s => s.GetGroupMembersAsync(It.IsAny<string>(), 1, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object>
                {
                    new { UserId = "user-1", Role = "admin" },
                    new { UserId = "user-2", Role = "member" }
                }));

            var response = await _client.GetAsync("/api/v1/Group/Members/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("admin").And.Contain("member");
        }

        [Fact]
        public async Task GetGroupMembers_GroupNotFound_Returns404()
        {
            _factory.GroupServiceMock
                .Setup(s => s.GetGroupMembersAsync(It.IsAny<string>(), 999, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Fail("Group not found", "NotFound"));

            var response = await _client.GetAsync("/api/v1/Group/Members/999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

 

        [Fact]
        public async Task GetGroupMessages_Member_Returns200WithMessages()
        {
            _factory.GroupServiceMock
                .Setup(s => s.GetGroupMessagesAsync(It.IsAny<string>(), 1, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<List<object>>.Ok(new List<object>
                {
                    new { Id = 1, Content = "Hello group!", SenderId = "user-1" }
                }));

            var response = await _client.GetAsync("/api/v1/Group/Messages/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Hello group!");
        }

        
   

        [Fact]
        public async Task DeleteMessageForEveryone_Sender_Returns200()
        {
            _factory.GroupServiceMock
                .Setup(s => s.DeleteMessageForEveryoneAsync(It.IsAny<string>(), 1))
                .ReturnsAsync(ServiceResult<object>.Ok("Message deleted for everyone"));

            var response = await _client.DeleteAsync("/api/v1/Group/DeleteMessageForEveryone/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

 

        [Fact]
        public async Task MarkAsRead_ValidMessage_Returns200()
        {
            _factory.GroupServiceMock
                .Setup(s => s.MarkAsReadAsync(It.IsAny<string>(), 1))
                .ReturnsAsync(ServiceResult<object>.Ok("Marked as read"));

            var response = await _client.PostAsJsonAsync("/api/v1/Group/MarkAsRead/1", new { });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }



        [Fact]
        public async Task WhoRead_ValidMessage_Returns200WithReaderList()
        {
            _factory.GroupServiceMock
                .Setup(s => s.WhoReadAsync(It.IsAny<string>(), 1, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object>
                {
                    new { UserId = "user-2", Name = "Alice" }
                }));

            var response = await _client.GetAsync("/api/v1/Group/WhoRead/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Alice");
        }

       

        [Fact]
        public async Task GetUnreadCount_Member_Returns200WithCount()
        {
            _factory.GroupServiceMock
                .Setup(s => s.GetUnreadCountAsync(It.IsAny<string>(), 1))
                .ReturnsAsync(ServiceResult<int>.Ok(7));

            var response = await _client.GetAsync("/api/v1/Group/UnreadCount/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("7");
        }

     

        [Fact]
        public async Task ClearChat_Member_Returns200()
        {
            _factory.GroupServiceMock
                .Setup(s => s.ClearChatAsync(It.IsAny<string>(), 1))
                .ReturnsAsync(ServiceResult<object>.Ok("Chat cleared for you"));

            var response = await _client.DeleteAsync("/api/v1/Group/ClearChat/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
}

    }

