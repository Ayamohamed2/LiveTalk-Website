using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.TestHelpers;
using Moq;
using NEEFRA.Core.DTO.Service;
using Restaurant.Core.DTO.Chat;
using Xunit;

namespace IntegrationTests
{
    public class OneToOneChatControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public OneToOneChatControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

  

        [Fact]
        public async Task GetAllUsers_Authenticated_Returns200WithList()
        {
            _factory.ChatServiceMock
                .Setup(s => s.GetUsersAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object>
                {
                    new { Id = "user-2", Name = "Alice" },
                    new { Id = "user-3", Name = "Bob" }
                }));

            var response = await _client.GetAsync("/api/v1/OneToOneChat/AllUsers");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Alice").And.Contain("Bob");
        }

        [Fact]
        public async Task GetAllUsers_Unauthenticated_Returns401()
        {
            var anonClient = _factory.CreateClient();
            var response = await anonClient.GetAsync("/api/v1/OneToOneChat/AllUsers");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }


        [Fact]
        public async Task GetMessagesWithUser_ValidUser_Returns200WithMessages()
        {
            _factory.ChatServiceMock
                .Setup(s => s.GetMessagesWithUserAsync(It.IsAny<string>(), "user-2", It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object>
                {
                    new { Id = 1, Content = "Hello!", SenderId = "user-1" },
                    new { Id = 2, Content = "How are you?", SenderId = "user-2" }
                }));

            var response = await _client.GetAsync("/api/v1/OneToOneChat/messages/user-2");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Hello!");
        }

        [Fact]
        public async Task GetMessagesWithUser_BlockedUser_Returns400()
        {
            _factory.ChatServiceMock
                .Setup(s => s.GetMessagesWithUserAsync(It.IsAny<string>(), "blocked-user", It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Fail("Cannot view messages with blocked user"));

            var response = await _client.GetAsync("/api/v1/OneToOneChat/messages/blocked-user");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetMessagesWithUser_NonExistentUser_Returns404()
        {
            _factory.ChatServiceMock
                .Setup(s => s.GetMessagesWithUserAsync(It.IsAny<string>(), "ghost-user", It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Fail("User not found", "NotFound"));

            var response = await _client.GetAsync("/api/v1/OneToOneChat/messages/ghost-user");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }



        [Fact]
        public async Task MarkAsDelivered_ValidMessage_Returns200()
        {
            _factory.ChatServiceMock
                .Setup(s => s.MarkAsDeliveredAsync(It.IsAny<string>(), 7))
                .ReturnsAsync(ServiceResult<object>.Ok("Marked as delivered"));

            var response = await _client.PostAsJsonAsync("/api/v1/OneToOneChat/MarkAsDelivered/7", new { });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task MarkAsDelivered_NotFound_Returns404()
        {
            _factory.ChatServiceMock
                .Setup(s => s.MarkAsDeliveredAsync(It.IsAny<string>(), 999))
                .ReturnsAsync(ServiceResult<object>.Fail("Message not found", "NotFound"));

            var response = await _client.PostAsJsonAsync("/api/v1/OneToOneChat/MarkAsDelivered/999", new { });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Fact]
        public async Task MarkAsRead_ValidSender_Returns200()
        {
            _factory.ChatServiceMock
                .Setup(s => s.MarkAsReadAsync(It.IsAny<string>(), "user-2"))
                .ReturnsAsync(ServiceResult<object>.Ok("All messages marked as read"));

            var response = await _client.PostAsJsonAsync("/api/v1/OneToOneChat/MarkAsRead/user-2", new { });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }


        [Fact]
        public async Task MarkSingleAsRead_ValidMessage_Returns200()
        {
            _factory.ChatServiceMock
                .Setup(s => s.MarkSingleAsReadAsync(It.IsAny<string>(), 1))
                .ReturnsAsync(ServiceResult<object>.Ok("Message marked as read"));

            var response = await _client.PostAsJsonAsync("/api/v1/OneToOneChat/MarkSingleAsRead/1", new { });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task MarkSingleAsRead_NotFound_Returns404()
        {
            _factory.ChatServiceMock
                .Setup(s => s.MarkSingleAsReadAsync(It.IsAny<string>(), 999))
                .ReturnsAsync(ServiceResult<object>.Fail("Message not found", "NotFound"));

            var response = await _client.PostAsJsonAsync("/api/v1/OneToOneChat/MarkSingleAsRead/999", new { });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

     

        [Fact]
        public async Task GetUnreadCount_ValidSender_Returns200WithCount()
        {
            _factory.ChatServiceMock
                .Setup(s => s.GetUnreadCountAsync(It.IsAny<string>(), "user-2"))
                .ReturnsAsync(ServiceResult<int>.Ok(5));

            var response = await _client.GetAsync("/api/v1/OneToOneChat/UnreadCount/user-2");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("5");
        }

        [Fact]
        public async Task GetUnreadCount_ZeroMessages_Returns200WithZero()
        {
            _factory.ChatServiceMock
                .Setup(s => s.GetUnreadCountAsync(It.IsAny<string>(), "user-3"))
                .ReturnsAsync(ServiceResult<int>.Ok(0));

            var response = await _client.GetAsync("/api/v1/OneToOneChat/UnreadCount/user-3");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("0");
        }



        [Fact]
        public async Task DeleteMessageForMe_ValidMessage_Returns200()
        {
            _factory.ChatServiceMock
                .Setup(s => s.DeleteMessageForMeAsync(It.IsAny<string>(), 3))
                .ReturnsAsync(ServiceResult<object>.Ok("Deleted for you"));

            var response = await _client.DeleteAsync("/api/v1/OneToOneChat/DeleteMessageForMe/3");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task DeleteMessageForMe_NotFound_Returns404()
        {
            _factory.ChatServiceMock
                .Setup(s => s.DeleteMessageForMeAsync(It.IsAny<string>(), 999))
                .ReturnsAsync(ServiceResult<object>.Fail("Message not found", "NotFound"));

            var response = await _client.DeleteAsync("/api/v1/OneToOneChat/DeleteMessageForMe/999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

       

        [Fact]
        public async Task DeleteMessageForEveryone_OwnMessage_Returns200()
        {
            _factory.ChatServiceMock
                .Setup(s => s.DeleteMessageForEveryoneAsync(It.IsAny<string>(), 1))
                .ReturnsAsync(ServiceResult<object>.Ok("Deleted for everyone"));

            var response = await _client.DeleteAsync("/api/v1/OneToOneChat/DeleteMessageForEveryone/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }


        [Fact]
        public async Task DeleteMessageForEveryone_NotFound_Returns404()
        {
            _factory.ChatServiceMock
                .Setup(s => s.DeleteMessageForEveryoneAsync(It.IsAny<string>(), 999))
                .ReturnsAsync(ServiceResult<object>.Fail("Message not found", "NotFound"));

            var response = await _client.DeleteAsync("/api/v1/OneToOneChat/DeleteMessageForEveryone/999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }



        [Fact]
        public async Task ClearChat_ValidUser_Returns200()
        {
            _factory.ChatServiceMock
                .Setup(s => s.ClearChatAsync(It.IsAny<string>(), "user-2"))
                .ReturnsAsync(ServiceResult<object>.Ok("Chat cleared"));

            var response = await _client.DeleteAsync("/api/v1/OneToOneChat/ClearChat/user-2");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ClearChat_NonExistentUser_Returns404()
        {
            _factory.ChatServiceMock
                .Setup(s => s.ClearChatAsync(It.IsAny<string>(), "ghost"))
                .ReturnsAsync(ServiceResult<object>.Fail("User not found", "NotFound"));

            var response = await _client.DeleteAsync("/api/v1/OneToOneChat/ClearChat/ghost");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        

        [Fact]
        public async Task BlockUser_ValidUser_Returns200()
        {
            _factory.ChatServiceMock
                .Setup(s => s.BlockUserAsync(It.IsAny<string>(), "user-2"))
                .ReturnsAsync(ServiceResult<object>.Ok("User blocked"));

            var response = await _client.PostAsJsonAsync("/api/v1/OneToOneChat/BlockUser/user-2", new { });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task BlockUser_AlreadyBlocked_Returns400()
        {
            _factory.ChatServiceMock
                .Setup(s => s.BlockUserAsync(It.IsAny<string>(), "user-2"))
                .ReturnsAsync(ServiceResult<object>.Fail("User is already blocked"));

            var response = await _client.PostAsJsonAsync("/api/v1/OneToOneChat/BlockUser/user-2", new { });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task BlockUser_BlockingYourself_Returns400()
        {
            _factory.ChatServiceMock
                .Setup(s => s.BlockUserAsync(It.IsAny<string>(), TestAuthHandler.TestUserId))
                .ReturnsAsync(ServiceResult<object>.Fail("Cannot block yourself"));

            var response = await _client.PostAsJsonAsync(
                $"/api/v1/OneToOneChat/BlockUser/{TestAuthHandler.TestUserId}", new { });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }



        [Fact]
        public async Task UnblockUser_BlockedUser_Returns200()
        {
            _factory.ChatServiceMock
                .Setup(s => s.UnblockUserAsync(It.IsAny<string>(), "user-2"))
                .ReturnsAsync(ServiceResult<object>.Ok("User unblocked"));

            var response = await _client.PostAsJsonAsync("/api/v1/OneToOneChat/UnblockUser/user-2", new { });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UnblockUser_NotBlocked_Returns404()
        {
            _factory.ChatServiceMock
                .Setup(s => s.UnblockUserAsync(It.IsAny<string>(), "user-3"))
                .ReturnsAsync(ServiceResult<object>.Fail("Block not found", "NotFound"));

            var response = await _client.PostAsJsonAsync("/api/v1/OneToOneChat/UnblockUser/user-3", new { });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

   

        [Fact]
        public async Task GetBlockedUsers_Returns200WithList()
        {
            _factory.ChatServiceMock
                .Setup(s => s.GetBlockedUsersAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object>
                {
                    new { Id = "user-3", Name = "Charlie" }
                }));

            var response = await _client.GetAsync("/api/v1/OneToOneChat/BlockedUsers");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Charlie");
        }

        [Fact]
        public async Task GetBlockedUsers_EmptyList_Returns200WithEmpty()
        {
            _factory.ChatServiceMock
                .Setup(s => s.GetBlockedUsersAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object>()));

            var response = await _client.GetAsync("/api/v1/OneToOneChat/BlockedUsers");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }



        [Fact]
        public async Task GetMyChatList_Returns200WithSortedList()
        {
            _factory.ChatServiceMock
                .Setup(s => s.GetMyChatListAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<ChatListItemDTO>>.Ok(new List<ChatListItemDTO>
                {
                    new() { UserId = "user-2", Name = "Alice", UnreadCount = 3 },
                    new() { UserId = "user-3", Name = "Bob", UnreadCount = 0 }
                }));

            var response = await _client.GetAsync("/api/v1/OneToOneChat/MyChatList");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Alice").And.Contain("Bob");
        }

        [Fact]
        public async Task GetMyChatList_Unauthenticated_Returns401()
        {
            var anonClient = _factory.CreateClient();
            var response = await anonClient.GetAsync("/api/v1/OneToOneChat/MyChatList");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
