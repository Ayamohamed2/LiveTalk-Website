using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NEEFRA.Core.DTO.Service;
using NEEFRA.Core.Services;
using Realtima_Chat_project.DataAccess.Reposatory.IReposatory;
using Realtima_Chat_project.DTO;
using Restaurant.Core.DTO.Chat;
using Restaurant.Core.Entity.Chat;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;
using Xunit;

namespace LiveTalkTests.UnitTesting.ServiceUnitTesting
{
    public class CallServiceTests
    {


        private readonly Mock<IUnitOfWork> _unitMock = new();
        private readonly Mock<ILogger<CallService>> _loggerMock = new();
        private readonly Mock<ICallReposatory> _callRepoMock = new();

        private readonly CallService _sut;

        public CallServiceTests()
        {
            _unitMock.Setup(u => u.Call).Returns(_callRepoMock.Object);
            _sut = new CallService(_unitMock.Object, _loggerMock.Object);
        }



        [Fact]
        public async Task SaveCallLogAsync_Always_CreatesCallAndReturnsSuccess()
        {
            var dto = new CallDTO
            {
                CallerId = "caller-1",
                ReceiverId = "receiver-1",
                CallType = 0,
                CallStatus = 1,
                StartedAt = DateTime.Now.AddMinutes(-2),
                EndedAt = DateTime.Now,
                Duration = 120
            };

            Call? capturedCall = null;
            _callRepoMock.Setup(r => r.Create(It.IsAny<Call>()))
                         .Callback<Call>(c => capturedCall = c);

            var result = await _sut.SaveCallLogAsync(dto);

            Assert.True(result.IsSuccess);
            Assert.NotNull(capturedCall);
            Assert.Equal("caller-1", capturedCall!.CallerId);
            Assert.Equal("receiver-1", capturedCall.ReceiverId);
            _unitMock.Verify(u => u.save(), Times.Once);
        }


        [Fact]
        public async Task GetCallHistoryAsync_ReturnsCallsForUser()
        {
            const string userId = "user-1";

            var calls = new List<Call>
            {
                new() { Id = 1, CallerId = userId, ReceiverId = "user-2", StartedAt = DateTime.Now },
                new() { Id = 2, CallerId = "user-3", ReceiverId = userId, StartedAt = DateTime.Now }
            };

            _callRepoMock
                .Setup(r => r.GetALL(It.IsAny<Expression<Func<Call, bool>>>(), null, 0, 1))
                .Returns(calls);

            var userRepoMock = new Mock<IAPPlicationUserReposatory>();
            userRepoMock.Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Restaurant.Core.Models.Account.ApplicationUser, bool>>>(), null))
                        .Returns(new Restaurant.Core.Models.Account.ApplicationUser { Name = "Test User" });
            _unitMock.Setup(u => u.User).Returns(userRepoMock.Object);

            var result = await _sut.GetCallHistoryAsync(userId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data!.Count());
        }

        [Fact]
        public async Task GetCallHistoryAsync_WhenNoCalls_ReturnsEmptyList()
        {
            _callRepoMock
                .Setup(r => r.GetALL(It.IsAny<Expression<Func<Call, bool>>>(), null, 0, 1))
                .Returns(new List<Call>());

            var userRepoMock = new Mock<IAPPlicationUserReposatory>();
            _unitMock.Setup(u => u.User).Returns(userRepoMock.Object);

            var result = await _sut.GetCallHistoryAsync("ghost-user");

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

    

        [Fact]
        public async Task GetCallHistoryWithUserAsync_ReturnsMutualCalls()
        {
            const string current = "user-1";
            const string other = "user-2";

            var calls = new List<Call>
            {
                new() { Id = 1, CallerId = current, ReceiverId = other, StartedAt = DateTime.Now },
                new() { Id = 2, CallerId = other,   ReceiverId = current, StartedAt = DateTime.Now }
            };

            _callRepoMock
                .Setup(r => r.GetALL(It.IsAny<Expression<Func<Call, bool>>>(), null, 0, 1))
                .Returns(calls);

            var result = await _sut.GetCallHistoryWithUserAsync(current, other);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Count());
        }

  

        [Fact]
        public async Task DeleteCallLogAsync_WhenCallNotFound_ReturnsNotFound()
        {
            _callRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Call, bool>>>(), null))
                .Returns((Call?)null);

            var result = await _sut.DeleteCallLogAsync("user-1", 99);

            Assert.False(result.IsSuccess);
            Assert.Equal("NotFound", result.ErrorType);
        }

        [Fact]
        public async Task DeleteCallLogAsync_WhenUserNotParticipant_ReturnsForbidden()
        {
            var call = new Call { Id = 1, CallerId = "other-1", ReceiverId = "other-2" };
            _callRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Call, bool>>>(), null))
                .Returns(call);

            var result = await _sut.DeleteCallLogAsync("intruder", 1);

            Assert.False(result.IsSuccess);
            Assert.Equal("Forbidden", result.ErrorType);
        }

        [Fact]
        public async Task DeleteCallLogAsync_WhenCallerDeletes_ReturnsSuccess()
        {
            var call = new Call { Id = 1, CallerId = "user-1", ReceiverId = "user-2" };
            _callRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Call, bool>>>(), null))
                .Returns(call);

            var result = await _sut.DeleteCallLogAsync("user-1", 1);

            Assert.True(result.IsSuccess);
            _callRepoMock.Verify(r => r.Delete(call), Times.Once);
            _unitMock.Verify(u => u.save(), Times.Once);
        }

        [Fact]
        public async Task DeleteCallLogAsync_WhenReceiverDeletes_ReturnsSuccess()
        {
            var call = new Call { Id = 2, CallerId = "user-1", ReceiverId = "user-2" };
            _callRepoMock
                .Setup(r => r.GetByFilter(It.IsAny<Expression<Func<Call, bool>>>(), null))
                .Returns(call);

            var result = await _sut.DeleteCallLogAsync("user-2", 2);

            Assert.True(result.IsSuccess);
        }
    }
}