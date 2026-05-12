using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.TestHelpers;
using Moq;
using NEEFRA.Core.DTO.Service;
using Realtima_Chat_project.DTO;
using Restaurant.Core.DTO.Chat;
using Xunit;

namespace IntegrationTests
{
    public class CallControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public CallControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

      

        [Fact]
        public async Task SaveCallLog_InvalidData_Returns400()
        {
            _factory.CallServiceMock
                .Setup(s => s.SaveCallLogAsync(It.IsAny<CallDTO>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Invalid call data"));

            var response = await _client.PostAsJsonAsync("/api/v1/Call/SaveCallLog", new { });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SaveCallLog_Unauthenticated_Returns401()
        {
            var anonClient = _factory.CreateClient();
            var response = await anonClient.PostAsJsonAsync("/api/v1/Call/SaveCallLog",
                new { ReceiverId = "user-2", Duration = 60 });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

     


        [Fact]
        public async Task GetCallHistory_Authenticated_Returns200WithList()
        {
            _factory.CallServiceMock
                .Setup(s => s.GetCallHistoryAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object>
                {
                    new { Id = 1, ReceiverId = "user-2", Duration = 120, CallType = "audio" },
                    new { Id = 2, ReceiverId = "user-3", Duration = 60, CallType = "video" }
                }));

            var response = await _client.GetAsync("/api/v1/Call/GetCallHistory");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("audio").And.Contain("video");
        }

        [Fact]
        public async Task GetCallHistory_NoHistory_Returns200WithEmptyList()
        {
            _factory.CallServiceMock
                .Setup(s => s.GetCallHistoryAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object>()));

            var response = await _client.GetAsync("/api/v1/Call/GetCallHistory");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetCallHistory_Unauthenticated_Returns401()
        {
            var anonClient = _factory.CreateClient();
            var response = await anonClient.GetAsync("/api/v1/Call/GetCallHistory");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }



        [Fact]
        public async Task GetCallHistoryWithUser_ValidUser_Returns200()
        {
            _factory.CallServiceMock
                .Setup(s => s.GetCallHistoryWithUserAsync(It.IsAny<string>(), "user-2"))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object>
                {
                    new { Id = 1, Duration = 90, CallType = "video" }
                }));

            var response = await _client.GetAsync("/api/v1/Call/GetCallHistoryWithUser/user-2");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("video");
        }

        [Fact]
        public async Task GetCallHistoryWithUser_NonExistentUser_Returns404()
        {
            _factory.CallServiceMock
                .Setup(s => s.GetCallHistoryWithUserAsync(It.IsAny<string>(), "ghost"))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Fail("User not found", "NotFound"));

            var response = await _client.GetAsync("/api/v1/Call/GetCallHistoryWithUser/ghost");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetCallHistoryWithUser_NoSharedCalls_Returns200WithEmpty()
        {
            _factory.CallServiceMock
                .Setup(s => s.GetCallHistoryWithUserAsync(It.IsAny<string>(), "user-5"))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(new List<object>()));

            var response = await _client.GetAsync("/api/v1/Call/GetCallHistoryWithUser/user-5");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

       

        [Fact]
        public async Task DeleteCallLog_OwnCall_Returns200()
        {
            _factory.CallServiceMock
                .Setup(s => s.DeleteCallLogAsync(It.IsAny<string>(), 42))
                .ReturnsAsync(ServiceResult<object>.Ok("Call log deleted"));

            var response = await _client.DeleteAsync("/api/v1/Call/DeleteCallLog/42");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task DeleteCallLog_NotFound_Returns404()
        {
            _factory.CallServiceMock
                .Setup(s => s.DeleteCallLogAsync(It.IsAny<string>(), 999))
                .ReturnsAsync(ServiceResult<object>.Fail("Call not found", "NotFound"));

            var response = await _client.DeleteAsync("/api/v1/Call/DeleteCallLog/999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Fact]
        public async Task DeleteCallLog_Unauthenticated_Returns401()
        {
            var anonClient = _factory.CreateClient();
            var response = await anonClient.DeleteAsync("/api/v1/Call/DeleteCallLog/1");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
