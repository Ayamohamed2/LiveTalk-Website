using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NEEFRA.Core.DTO.Service;
using Restaurant.Core.DTO.Acoount;
using Restaurant.Core.DTO.Email;
using Restaurant.Core.Interfaces.IService;
using System.Security.Claims;
using Villa_API_Project.Controllers.V2;
using Xunit;

namespace LiveTalkTests.UnitTesting.ControllerUnitTesting
{

    public class AccountControllerTests
    {

        private readonly Mock<IAccountService> _serviceMock = new();

        private AccountController CreateSut(string userId = "user-1")
        {
            var controller = new AccountController(_serviceMock.Object);

           
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            controller.ControllerContext.HttpContext.Request.Scheme = "https";
            controller.ControllerContext.HttpContext.Request.Host = new HostString("example.com");

            return controller;
        }


        [Fact]
        public async Task Register_Success_Returns200WithData()
        {
            // Arrange
            var dto = new RegisterDTO { Email = "new@test.com", Password = "Pass@1234" };
            _serviceMock
                .Setup(s => s.RegisterAsync(dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Ok("Registered successfully", "Check your email"));

            // Act
            var result = await CreateSut().Register(dto) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task Register_Failure_Returns400()
        {
            // Arrange
            var dto = new RegisterDTO { Email = "bad@test.com", Password = "weak" };
            _serviceMock
                .Setup(s => s.RegisterAsync(dto, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Fail("Password too short", "BadRequest"));

            // Act
            var result = await CreateSut().Register(dto) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }


        [Fact]
        public async Task Login_ValidCredentials_Returns200WithToken()
        {
            // Arrange
            var dto = new LoginDTO { Email = "ok@test.com", Password = "Pass@1234" };
            var token = new TokenDTO { AccessToken = "jwt-access", RefreshToken = "jwt-refresh" };
            _serviceMock
                .Setup(s => s.LoginAsync(dto))
                .ReturnsAsync(ServiceResult<object>.Ok(token, "Login successful"));

            // Act
            var result = await CreateSut().Login(dto) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task Login_InvalidCredentials_Returns401()
        {
            // Arrange
            var dto = new LoginDTO { Email = "x@test.com", Password = "wrong" };
            _serviceMock
                .Setup(s => s.LoginAsync(dto))
                .ReturnsAsync(ServiceResult<object>.Fail("Invalid credentials", "Unauthorized"));

            // Act
            var result = await CreateSut().Login(dto) as ObjectResult;

            // Assert – HandleResult maps "Unauthorized" → 401
            Assert.NotNull(result);
            Assert.Equal(401, result!.StatusCode);
        }


        [Fact]
        public async Task Refresh_ValidToken_Returns200()
        {
            // Arrange
            var dto = new TokenDTO { AccessToken = "old", RefreshToken = "old-r" };
            var newToken = new TokenDTO { AccessToken = "new", RefreshToken = "new-r" };
            _serviceMock
                .Setup(s => s.RefreshTokenAsync(dto))
                .ReturnsAsync(ServiceResult<TokenDTO>.Ok(newToken));

            // Act
            var result = await CreateSut().Refresh(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task Refresh_InvalidToken_Returns400()
        {
            // Arrange
            var dto = new TokenDTO { AccessToken = "bad", RefreshToken = "bad-r" };
            _serviceMock
                .Setup(s => s.RefreshTokenAsync(dto))
                .ReturnsAsync(ServiceResult<TokenDTO>.Fail("Invalid token", "BadRequest"));

            // Act
            var result = await CreateSut().Refresh(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }


        [Fact]
        public async Task Logout_ValidToken_Returns200()
        {
            // Arrange
            var dto = new TokenDTO { RefreshToken = "r-token" };
            _serviceMock
                .Setup(s => s.LogoutAsync(dto))
                .ReturnsAsync(ServiceResult<string>.Ok("Logged out"));

            // Act
            var result = await CreateSut().Logout(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task ConfirmEmail_ValidParams_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.ConfirmEmailAsync("user-1", "valid-token"))
                .ReturnsAsync(ServiceResult<string>.Ok("Email confirmed"));

            // Act
            var result = await CreateSut().ConfirmEmail("user-1", "valid-token") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task ConfirmEmail_UserNotFound_Returns404()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.ConfirmEmailAsync("ghost", It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Fail("User not found", "NotFound"));

            // Act
            var result = await CreateSut().ConfirmEmail("ghost", "token") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }

        // ─── ForgetPassword ───────────────────────────────────────────────────

        [Fact]
        public async Task ForgetPassword_KnownEmail_Returns200()
        {
            // Arrange
            var email = new EmailDTO { Email = "known@test.com" };
            _serviceMock
                .Setup(s => s.ForgetPasswordAsync(email, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Ok("Reset email sent"));

            // Act
            var result = await CreateSut().ForgetPassword(email) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task ForgetPassword_UnknownEmail_Returns404()
        {
            // Arrange
            var email = new EmailDTO { Email = "ghost@test.com" };
            _serviceMock
                .Setup(s => s.ForgetPasswordAsync(email, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Fail("Email not found", "NotFound"));

            // Act
            var result = await CreateSut().ForgetPassword(email) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }


        [Fact]
        public async Task ResetPassword_Valid_Returns200()
        {
            // Arrange
            var model = new ResetPasswordDTO { NewPassword = "NewPass@1", ConfirmPassword = "NewPass@1" };
            _serviceMock
                .Setup(s => s.ResetPasswordAsync("user@test.com", "token", model))
                .ReturnsAsync(ServiceResult<string>.Ok("Password reset"));

            // Act
            var result = await CreateSut().ResetPassword("user@test.com", "token", model) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_InvalidToken_Returns400()
        {
            // Arrange
            var model = new ResetPasswordDTO { NewPassword = "NewPass@1", ConfirmPassword = "NewPass@1" };
            _serviceMock
                .Setup(s => s.ResetPasswordAsync(It.IsAny<string>(), "bad-token", model))
                .ReturnsAsync(ServiceResult<string>.Fail("Invalid token", "BadRequest"));

            // Act
            var result = await CreateSut().ResetPassword("user@test.com", "bad-token", model) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }


        [Fact]
        public async Task ChangeEmail_ValidRequest_Returns200()
        {
            // Arrange
            var email = new EmailDTO { Email = "new@test.com" };
            _serviceMock
                .Setup(s => s.ChangeEmailAsync("user-1", email, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Ok("Confirmation email sent"));

            // Act
            var result = await CreateSut("user-1").ChangeEmail(email) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }


        [Fact]
        public async Task ChangePassword_ValidRequest_Returns200()
        {
            // Arrange
            var model = new ChangePasswordDTO { CurrentPassword = "old", NewPassword = "NewPass@1" };
            _serviceMock
                .Setup(s => s.ChangePasswordAsync("user-1", model))
                .ReturnsAsync(ServiceResult<string>.Ok("Password changed"));

            // Act
            var result = await CreateSut("user-1").ChangePassword(model) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_Returns400()
        {
            // Arrange
            var model = new ChangePasswordDTO { CurrentPassword = "wrong", NewPassword = "NewPass@1" };
            _serviceMock
                .Setup(s => s.ChangePasswordAsync("user-1", model))
                .ReturnsAsync(ServiceResult<string>.Fail("Incorrect password", "BadRequest"));

            // Act
            var result = await CreateSut("user-1").ChangePassword(model) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }


        [Fact]
        public async Task Verify2FA_ValidCode_Returns200WithToken()
        {
            // Arrange
            var token = new TokenDTO { AccessToken = "access-jwt" };
            _serviceMock
                .Setup(s => s.VerifyTwoFactorAsync("user-1", "123456"))
                .ReturnsAsync(ServiceResult<TokenDTO>.Ok(token));

            // Act
            var result = await CreateSut().Verify2FA("123456", "user-1") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task Verify2FA_InvalidCode_Returns400()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.VerifyTwoFactorAsync("user-1", "badcode"))
                .ReturnsAsync(ServiceResult<TokenDTO>.Fail("Invalid OTP", "BadRequest"));

            // Act
            var result = await CreateSut().Verify2FA("badcode", "user-1") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }


        [Fact]
        public async Task Enable2FA_UserExists_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.EnableTwoFactorAsync("user-1"))
                .ReturnsAsync(ServiceResult<string>.Ok("2FA enabled"));

            // Act
            var result = await CreateSut("user-1").Enable2FA() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task Disable2FA_UserExists_Returns200()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DisableTwoFactorAsync("user-1"))
                .ReturnsAsync(ServiceResult<string>.Ok("2FA disabled"));

            // Act
            var result = await CreateSut("user-1").Disable2FA() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task Enable2FA_UserNotFound_Returns400()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.EnableTwoFactorAsync("ghost"))
                .ReturnsAsync(ServiceResult<string>.Fail("User not found", "BadRequest"));

            // Act
            var result = await CreateSut("ghost").Enable2FA() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }
    }
}
