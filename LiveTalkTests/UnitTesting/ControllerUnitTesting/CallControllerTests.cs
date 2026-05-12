
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

namespace LiveTalkTests.UnitTesting.ControllerUnitTesting
{
    public class CallControllerTests
    {

        private readonly Mock<ICallService> _serviceMock = new();

        private CallController CreateSut(string userId = "user-1")
        {
            var controller = new CallController(_serviceMock.Object);
            controller.ControllerContext = BuildContext(userId);
            return controller;
        }

        private static ControllerContext BuildContext(string userId)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpCtx = new DefaultHttpContext { User = principal };
            httpCtx.Request.Scheme = "https";
            httpCtx.Request.Host = new HostString("example.com");
            return new ControllerContext { HttpContext = httpCtx };
        }


        [Fact]
        public async Task SaveCallLog_ValidDto_Returns200()
        {
            // Arrange
            var dto = new CallDTO
            {
                CallerId = "user-1",
                ReceiverId = "user-2",
                CallType = 0,
                CallStatus = 1,
                StartedAt = DateTime.UtcNow.AddMinutes(-5),
                EndedAt = DateTime.UtcNow,
                Duration = 300
            };
            _serviceMock
                .Setup(s => s.SaveCallLogAsync(dto))
                .ReturnsAsync(ServiceResult<object>.Ok(dto, "Call log saved"));

            // Act
            var result = await CreateSut().SaveCallLog(dto) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task SaveCallLog_ServiceFails_Returns400()
        {
            // Arrange
            var dto = new CallDTO { CallerId = "user-1", ReceiverId = "user-2" };
            _serviceMock
                .Setup(s => s.SaveCallLogAsync(dto))
                .ReturnsAsync(ServiceResult<object>.Fail("Invalid data", "BadRequest"));

            // Act
            var result = await CreateSut().SaveCallLog(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }


        [Fact]
        public async Task GetCallHistory_HasLogs_Returns200WithList()
        {
            // Arrange
            var logs = new List<CallDTO> { new() { CallerId = "user-1", ReceiverId = "user-2" } };
            _serviceMock
                .Setup(s => s.GetCallHistoryAsync("user-1"))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(logs));

            // Act
            var result = await CreateSut("user-1").GetCallHistory() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetCallHistory_NoLogs_Returns200WithEmptyList()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetCallHistoryAsync("user-1"))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(Enumerable.Empty<CallDTO>()));

            // Act
            var result = await CreateSut("user-1").GetCallHistory() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task GetCallHistoryWithUser_ValidPair_Returns200()
        {
            // Arrange
            var logs = new List<CallDTO>
            {
                new() { CallerId = "user-1", ReceiverId = "user-2" },
                new() { CallerId = "user-2", ReceiverId = "user-1" }
            };
            _serviceMock
                .Setup(s => s.GetCallHistoryWithUserAsync("user-1", "user-2"))
                .ReturnsAsync(ServiceResult<IEnumerable<object>>.Ok(logs));

            // Act
            var result = await CreateSut("user-1").GetCallHistoryWithUser("user-2") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task DeleteCallLog_Owner_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteCallLogAsync("user-1", 42))
                .ReturnsAsync(ServiceResult<object>.Ok("Deleted"));

            // Act
            var result = await CreateSut("user-1").DeleteCallLog(42) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task DeleteCallLog_NotFound_Returns404()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteCallLogAsync("user-1", 999))
                .ReturnsAsync(ServiceResult<object>.Fail("Call not found", "NotFound"));

            // Act
            var result = await CreateSut("user-1").DeleteCallLog(999) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }

        [Fact]
        public async Task DeleteCallLog_Unauthorized_Returns403()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteCallLogAsync("intruder", 10))
                .ReturnsAsync(ServiceResult<object>.Fail("Forbidden", "Forbidden"));

            // Act
            var raw = await CreateSut("intruder").DeleteCallLog(10);
            Assert.IsType<ForbidResult>(raw);
        }
    }
}




