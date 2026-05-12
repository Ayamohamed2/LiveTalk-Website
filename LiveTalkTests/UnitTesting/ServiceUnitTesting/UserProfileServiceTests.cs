using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NEEFRA.Core.DTO.Service;
using Restaurant.Core.DTO.Profie;
using Restaurant.Core.Interfaces.IService.Redis;
using Restaurant.Core.Models.Account;
using Restaurant.Core.Services;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;
using Xunit;

namespace LiveTalkTests.UnitTesting.ServiceUnitTesting
{
    public class UserProfileServiceTests
    {
    

        private readonly Mock<IUnitOfWork> _unitMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<ILogger<UserProfileService>> _loggerMock = new();
        private readonly Mock<IRedisCacheService> _cacheMock = new();
        private readonly Mock<IWebHostEnvironment> _envMock = new();

        private readonly UserProfileService _sut;

        public UserProfileServiceTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _sut = new UserProfileService(
                _unitMock.Object,
                _userManagerMock.Object,
                _loggerMock.Object,
                _cacheMock.Object);
        }

       

        [Fact]
        public async Task GetProfileAsync_WhenCacheHit_ReturnsProfileWithoutDbCall()
        {
            const string userId = "u1";
            const string baseUrl = "http://localhost";
            var cachedUser = new ApplicationUser
            {
                Id = userId,
                Email = "cached@x.com",
                Name = "Cached User",
                PhoneNumber = "01000000000",
                ImageURL = "/Images/profile.png"
            };

            _cacheMock.Setup(c => c.GetAsync<ApplicationUser>($"Profile:{userId}"))
                      .ReturnsAsync(cachedUser);

            var result = await _sut.GetProfileAsync(userId, baseUrl);

            Assert.True(result.IsSuccess);
            _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetProfileAsync_WhenCacheMiss_QueriesDbAndCaches()
        {
            const string userId = "u1";
            const string baseUrl = "http://localhost";
            var user = new ApplicationUser
            {
                Id = userId,
                Email = "real@x.com",
                Name = "Real User",
                PhoneNumber = "01111111111",
                ImageURL = "/Images/avatar.png"
            };

            _cacheMock.Setup(c => c.GetAsync<ApplicationUser>($"Profile:{userId}"))
                      .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

            var result = await _sut.GetProfileAsync(userId, baseUrl);

            Assert.True(result.IsSuccess);
            _cacheMock.Verify(c => c.SetAsync(
                $"Profile:{userId}", It.IsAny<object>(), TimeSpan.FromMinutes(30)), Times.Once);
        }

        [Fact]
        public async Task GetProfileAsync_WhenUserNotFound_ReturnsBadRequest()
        {
            const string userId = "ghost";
            _cacheMock.Setup(c => c.GetAsync<ApplicationUser>($"Profile:{userId}"))
                      .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(m => m.FindByIdAsync(userId))
                            .ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.GetProfileAsync(userId, "http://localhost");

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
        }

        [Fact]
        public async Task GetProfileAsync_WhenNoImage_ImageUrlIsNull()
        {
            const string userId = "u2";
            const string baseUrl = "http://localhost";
            var user = new ApplicationUser { Id = userId, Email = "x@x.com", ImageURL = null };

            _cacheMock.Setup(c => c.GetAsync<ApplicationUser>($"Profile:{userId}"))
                      .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

            var result = await _sut.GetProfileAsync(userId, baseUrl);

            Assert.True(result.IsSuccess);
        }

      

        [Fact]
        public async Task UpdateProfileAsync_WhenUserNotFound_ReturnsBadRequest()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("no-id"))
                            .ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.UpdateProfileAsync(
                "no-id",
                new UserProfileDTO { Name = "Test" },
                "http://localhost",
                _envMock.Object);

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenNameProvided_UpdatesName()
        {
            const string userId = "u1";
            var user = new ApplicationUser { Id = userId, Email = "x@x.com", Name = "Old Name", ImageURL = "/Images/default.png" };
            var dto = new UserProfileDTO { Name = "New Name", phoneNumber = "01234567890" };

            var userRepoMock = new Mock<IAPPlicationUserReposatory>();
            _unitMock.Setup(u => u.User).Returns(userRepoMock.Object);

            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _sut.UpdateProfileAsync(userId, dto, "http://localhost", _envMock.Object);

            Assert.True(result.IsSuccess);
            Assert.Equal("New Name", user.Name);
        }

        [Fact]
        public async Task UpdateProfileAsync_AfterUpdate_InvalidatesCache()
        {
            const string userId = "u1";
            var user = new ApplicationUser { Id = userId, Email = "x@x.com", Name = "N", ImageURL = "/Images/default.png" };
            var dto = new UserProfileDTO { Name = "Updated" };

            var userRepoMock = new Mock<IAPPlicationUserReposatory>();
            _unitMock.Setup(u => u.User).Returns(userRepoMock.Object);
            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            await _sut.UpdateProfileAsync(userId, dto, "http://localhost", _envMock.Object);

            _cacheMock.Verify(c => c.RemoveAsync($"Profile:{userId}"), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenPhoneNumberProvided_UpdatesPhone()
        {
            const string userId = "u1";
            var user = new ApplicationUser { Id = userId, Email = "x@x.com", PhoneNumber = "000", ImageURL = "/Images/default.png" };
            var dto = new UserProfileDTO { phoneNumber = "01099999999" };

            var userRepoMock = new Mock<IAPPlicationUserReposatory>();
            _unitMock.Setup(u => u.User).Returns(userRepoMock.Object);
            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _sut.UpdateProfileAsync(userId, dto, "http://localhost", _envMock.Object);

            Assert.True(result.IsSuccess);
            Assert.Equal("01099999999", user.PhoneNumber);
        }
    }
}