namespace LiveTalkTests.UnitTesting.ControllerUnitTesting
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NEEFRA.Core.DTO.Service;
    using Restaurant.Core.DTO.Profie;
    using Restaurant.Core.Interfaces.IService;
    using System.Security.Claims;
    using Villa_API_Project.Controllers;
    using Xunit;

    public class UserProfileControllerTests
    {

        private readonly Mock<IUserProfileService> _serviceMock = new();

        private UserProfileController CreateSut(string userId = "user-1")
        {
            var controller = new UserProfileController(_serviceMock.Object);
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var httpCtx = new DefaultHttpContext { User = principal };
            httpCtx.Request.Scheme = "https";
            httpCtx.Request.Host = new HostString("example.com");

            // UpdateProfileAsync needs IWebHostEnvironment from IServiceProvider.
            // We register a minimal DI container on the HttpContext.
            var services = new ServiceCollection();
            services.AddSingleton(new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().Object);
            httpCtx.RequestServices = services.BuildServiceProvider();

            controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };
            return controller;
        }


        [Fact]
        public async Task GetProfile_CacheHit_Returns200()
        {
            // Arrange
            var profile = new { Id = "user-1", Name = "Test User" };
            _serviceMock
                .Setup(s => s.GetProfileAsync("user-1", It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok(profile));

            // Act
            var result = await CreateSut("user-1").GetProfile() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetProfile_UserNotFound_Returns404()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetProfileAsync("ghost", It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("User not found", "NotFound"));

            // Act
            var result = await CreateSut("ghost").GetProfile() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }


        [Fact]
        public async Task UpdateProfile_ValidDto_Returns200()
        {
            // Arrange
            var dto = new UserProfileDTO { Name = "New Name" };
            _serviceMock
                .Setup(s => s.UpdateProfileAsync(
                    "user-1",
                    dto,
                    It.IsAny<string>(),
                    It.IsAny<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()))
                .ReturnsAsync(ServiceResult<object>.Ok(new { }, "Profile updated"));

            // Act
            var result = await CreateSut("user-1").UpdateProfile(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_UserNotFound_Returns400()
        {
            // Arrange
            var dto = new UserProfileDTO { Name = "X" };
            _serviceMock
                .Setup(s => s.UpdateProfileAsync(
                    "ghost",
                    dto,
                    It.IsAny<string>(),
                    It.IsAny<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()))
                .ReturnsAsync(ServiceResult<object>.Fail("User not found", "BadRequest"));

            // Act
            var result = await CreateSut("ghost").UpdateProfile(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_PhoneNumberUpdated_Returns200()
        {
            // Arrange
            var dto = new UserProfileDTO { phoneNumber = "01099999999" };
            _serviceMock
                .Setup(s => s.UpdateProfileAsync(
                    "user-1",
                    dto,
                    It.IsAny<string>(),
                    It.IsAny<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()))
                .ReturnsAsync(ServiceResult<object>.Ok(new { }, "Profile updated"));

            // Act
            var result = await CreateSut("user-1").UpdateProfile(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_IdentityFailure_Returns400()
        {
            // Arrange
            var dto = new UserProfileDTO { Name = "Crash" };
            _serviceMock
                .Setup(s => s.UpdateProfileAsync(
                    "user-1",
                    dto,
                    It.IsAny<string>(),
                    It.IsAny<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Update failed", "BadRequest"));

            // Act
            var result = await CreateSut("user-1").UpdateProfile(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }
    }
}