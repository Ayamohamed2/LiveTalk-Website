using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NEEFRA.Core.DTO.Service;
using Restaurant.Core.DTO.Acoount;
using Restaurant.Core.DTO.Email;
using Restaurant.Core.Interfaces.IService;
using Restaurant.Core.Models.Account;
using Restaurant.Core.Services;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;
using Xunit;

namespace LiveTalkTests.UnitTesting.ServiceUnitTesting
{
    public class AccountServiceTests
    {
     

        private readonly Mock<IUnitOfWork> _unitMock = new();
        private readonly Mock<IAuthRepository> _authMock = new();
        private readonly Mock<IEmailSender> _emailMock = new();
        private readonly Mock<IJWT_TokenReposatory> _jwtMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<ILogger<AccountService>> _loggerMock = new();

        private readonly AccountService _sut;

        public AccountServiceTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _sut = new AccountService(
                _unitMock.Object,
                _authMock.Object,
                _emailMock.Object,
                _jwtMock.Object,
                _userManagerMock.Object,
                _loggerMock.Object);
        }

      

        [Fact]
        public async Task RegisterAsync_WhenSucceeded_ReturnsSuccess()
        {
            var model = new RegisterDTO { Email = "test@test.com" };
            _authMock.Setup(a => a.Register(model, It.IsAny<string>()))
                     .ReturnsAsync(IdentityResult.Success);

            var result = await _sut.RegisterAsync(model, "http://localhost");

            Assert.True(result.IsSuccess);
            Assert.Contains("registered successfully", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RegisterAsync_WhenFailed_ReturnsFailure()
        {
            var model = new RegisterDTO { Email = "bad@test.com" };
            var identityErrors = new[] { new IdentityError { Description = "Email already taken" } };
            _authMock.Setup(a => a.Register(model, It.IsAny<string>()))
                     .ReturnsAsync(IdentityResult.Failed(identityErrors));

            var result = await _sut.RegisterAsync(model, "http://localhost");

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
            Assert.Contains("Email already taken", result.Message);
        }

        // ─────────────────────────────────────────────
        // LoginAsync
        // ─────────────────────────────────────────────

        [Fact]
        public async Task LoginAsync_WhenInvalidCredentials_ReturnsUnauthorized()
        {
            var dto = new LoginDTO { Email = "x@x.com", Password = "wrong" };
            _authMock.Setup(a => a.LoginAsync(dto))
                     .ReturnsAsync(new TokenDTO { Message = "null" });

            var result = await _sut.LoginAsync(dto);

            Assert.False(result.IsSuccess);
            Assert.Equal("Unauthorized", result.ErrorType);
        }

        [Fact]
        public async Task LoginAsync_WhenEmailNotConfirmed_ReturnsBadRequest()
        {
            var dto = new LoginDTO { Email = "x@x.com" };
            _authMock.Setup(a => a.LoginAsync(dto))
                     .ReturnsAsync(new TokenDTO { Message = "EmailNotConfirmed" });

            var result = await _sut.LoginAsync(dto);

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
            Assert.Equal("EmailNotConfirmed", result.Message);
        }

        [Fact]
        public async Task LoginAsync_WhenLockedOut_ReturnsBadRequest()
        {
            var dto = new LoginDTO { Email = "x@x.com" };
            _authMock.Setup(a => a.LoginAsync(dto))
                     .ReturnsAsync(new TokenDTO { Message = "LockedOut" });

            var result = await _sut.LoginAsync(dto);

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
            Assert.Contains("LockedOut", result.Message);
        }

        [Fact]
        public async Task LoginAsync_WhenTwoFactorEnabled_SendsOtpAndReturnsUserId()
        {
            var dto = new LoginDTO { Email = "otp@test.com" };
            var user = new ApplicationUser { Id = "user-1", Email = dto.Email, TwoFactorEnabled = true };

            _authMock.Setup(a => a.LoginAsync(dto))
                     .ReturnsAsync(new TokenDTO { Message = "ok", AccessToken = "token" });
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider))
                            .ReturnsAsync("123456");

            var result = await _sut.LoginAsync(dto);

            Assert.True(result.IsSuccess);
            Assert.Contains("OTP", result.Message, StringComparison.OrdinalIgnoreCase);
            _emailMock.Verify(e => e.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WhenNormal_ReturnsToken()
        {
            var dto = new LoginDTO { Email = "normal@test.com" };
            var tokenDto = new TokenDTO { Message = "ok", AccessToken = "access", RefreshToken = "refresh" };
            var user = new ApplicationUser { Id = "u1", Email = dto.Email, TwoFactorEnabled = false };

            _authMock.Setup(a => a.LoginAsync(dto)).ReturnsAsync(tokenDto);
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);

            var result = await _sut.LoginAsync(dto);

            Assert.True(result.IsSuccess);
        }


        [Fact]
        public async Task RefreshTokenAsync_WhenTokenValid_ReturnsNewToken()
        {
            var dto = new TokenDTO { AccessToken = "old", RefreshToken = "old_refresh" };
            var newToken = new TokenDTO { AccessToken = "new_access", RefreshToken = "new_refresh" };

            _jwtMock.Setup(j => j.RefreshAccessToken(dto)).ReturnsAsync(newToken);

            var result = await _sut.RefreshTokenAsync(dto);

            Assert.True(result.IsSuccess);
            Assert.Equal("new_access", result.Data?.AccessToken);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenTokenInvalid_ReturnsFailure()
        {
            var dto = new TokenDTO { AccessToken = "invalid" };
            _jwtMock.Setup(j => j.RefreshAccessToken(dto)).ReturnsAsync((TokenDTO?)null);

            var result = await _sut.RefreshTokenAsync(dto);

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
        }


        [Fact]
        public async Task LogoutAsync_AlwaysRevokesAndReturnsSuccess()
        {
            var dto = new TokenDTO { AccessToken = "a", RefreshToken = "r" };
            _jwtMock.Setup(j => j.RevokeAllTokens(dto)).Returns(Task.CompletedTask);

            var result = await _sut.LogoutAsync(dto);

            Assert.True(result.IsSuccess);
            _jwtMock.Verify(j => j.RevokeAllTokens(dto), Times.Once);
        }


        [Fact]
        public async Task ConfirmEmailAsync_WhenMissingParams_ReturnsBadRequest()
        {
            var result = await _sut.ConfirmEmailAsync("", "");

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
        }

        [Fact]
        public async Task ConfirmEmailAsync_WhenUserNotFound_ReturnsNotFound()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("no-user")).ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.ConfirmEmailAsync("no-user", "token");

            Assert.False(result.IsSuccess);
            Assert.Equal("NotFound", result.ErrorType);
        }

        [Fact]
        public async Task ConfirmEmailAsync_WhenTokenValid_ReturnsSuccess()
        {
            var user = new ApplicationUser { Id = "u1", Email = "e@e.com" };
            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, "valid-token"))
                            .ReturnsAsync(IdentityResult.Success);

            var result = await _sut.ConfirmEmailAsync("u1", "valid-token");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ConfirmEmailAsync_WhenTokenInvalid_ReturnsBadRequest()
        {
            var user = new ApplicationUser { Id = "u1", Email = "e@e.com" };
            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, "bad-token"))
                            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

            var result = await _sut.ConfirmEmailAsync("u1", "bad-token");

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
        }

      
        [Fact]
        public async Task ForgetPasswordAsync_WhenEmailNotFound_ReturnsNotFound()
        {
            var emailDto = new EmailDTO { Email = "ghost@test.com" };
            _userManagerMock.Setup(m => m.FindByEmailAsync(emailDto.Email)).ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.ForgetPasswordAsync(emailDto, "http://localhost");

            Assert.False(result.IsSuccess);
            Assert.Equal("NotFound", result.ErrorType);
        }

        [Fact]
        public async Task ForgetPasswordAsync_WhenEmailFound_SendsEmailAndReturnsSuccess()
        {
            var emailDto = new EmailDTO { Email = "real@test.com" };
            var user = new ApplicationUser { Id = "u1", Email = emailDto.Email };

            _userManagerMock.Setup(m => m.FindByEmailAsync(emailDto.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");

            var result = await _sut.ForgetPasswordAsync(emailDto, "http://localhost");

            Assert.True(result.IsSuccess);
            _emailMock.Verify(e => e.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }


        [Fact]
        public async Task ResetPasswordAsync_WhenUserNotFound_ReturnsBadRequest()
        {
            _userManagerMock.Setup(m => m.FindByEmailAsync("none@x.com")).ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.ResetPasswordAsync("none@x.com", "tok", new ResetPasswordDTO { NewPassword = "New@123" });

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenSuccess_ReturnsSuccess()
        {
            var user = new ApplicationUser { Id = "u1", Email = "real@x.com" };
            _userManagerMock.Setup(m => m.FindByEmailAsync("real@x.com")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ResetPasswordAsync(user, It.IsAny<string>(), "New@123"))
                            .ReturnsAsync(IdentityResult.Success);

            var result = await _sut.ResetPasswordAsync("real@x.com", "token", new ResetPasswordDTO { NewPassword = "New@123" });

            Assert.True(result.IsSuccess);
        }



        [Fact]
        public async Task ChangePasswordAsync_WhenUserNotFound_ReturnsBadRequest()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("no-id")).ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.ChangePasswordAsync("no-id", new ChangePasswordDTO());

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenSuccess_ReturnsSuccess()
        {
            var user = new ApplicationUser { Id = "u1" };
            var dto = new ChangePasswordDTO { CurrentPassword = "Old@1", NewPassword = "New@1" };

            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ChangePasswordAsync(user, "Old@1", "New@1"))
                            .ReturnsAsync(IdentityResult.Success);

            var result = await _sut.ChangePasswordAsync("u1", dto);

            Assert.True(result.IsSuccess);
        }

     

        [Fact]
        public async Task VerifyTwoFactorAsync_WhenUserNotFound_ReturnsFailure()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("no-id")).ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.VerifyTwoFactorAsync("no-id", "123456");

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task VerifyTwoFactorAsync_WhenCodeInvalid_ReturnsFailure()
        {
            var user = new ApplicationUser { Id = "u1" };
            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, "wrong"))
                            .ReturnsAsync(false);

            var result = await _sut.VerifyTwoFactorAsync("u1", "wrong");

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task VerifyTwoFactorAsync_WhenCodeValid_ReturnsToken()
        {
            var user = new ApplicationUser { Id = "u1" };
            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, "123456"))
                            .ReturnsAsync(true);
            _jwtMock.Setup(j => j.GenerateToken(user, It.IsAny<string>())).ReturnsAsync("access-token");
            _jwtMock.Setup(j => j.CreateNewRefreshToken(user.Id, It.IsAny<string>())).Returns("refresh-token");

            var result = await _sut.VerifyTwoFactorAsync("u1", "123456");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data?.AccessToken);
        }

   

        [Fact]
        public async Task EnableTwoFactorAsync_WhenUserNotFound_ReturnsFailure()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("no-id")).ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.EnableTwoFactorAsync("no-id");

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task EnableTwoFactorAsync_WhenUserFound_EnablesAndReturnsSuccess()
        {
            var user = new ApplicationUser { Id = "u1", TwoFactorEnabled = false };
            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _sut.EnableTwoFactorAsync("u1");

            Assert.True(result.IsSuccess);
            Assert.True(user.TwoFactorEnabled);
        }

        [Fact]
        public async Task DisableTwoFactorAsync_WhenUserFound_DisablesAndReturnsSuccess()
        {
            var user = new ApplicationUser { Id = "u1", TwoFactorEnabled = true };
            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _sut.DisableTwoFactorAsync("u1");

            Assert.True(result.IsSuccess);
            Assert.False(user.TwoFactorEnabled);
        }



        [Fact]
        public async Task ChangeEmailAsync_WhenUserNotFound_ReturnsBadRequest()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("no-id")).ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.ChangeEmailAsync("no-id", new EmailDTO { Email = "new@x.com" }, "http://localhost");

            Assert.False(result.IsSuccess);
            Assert.Equal("BadRequest", result.ErrorType);
        }

        [Fact]
        public async Task ChangeEmailAsync_WhenEmailAlreadyUsed_ReturnsBadRequest()
        {
            var user = new ApplicationUser { Id = "u1", Email = "old@x.com" };
            var otherUser = new ApplicationUser { Id = "u2", Email = "new@x.com" };

            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.FindByEmailAsync("new@x.com")).ReturnsAsync(otherUser);

            var result = await _sut.ChangeEmailAsync("u1", new EmailDTO { Email = "new@x.com" }, "http://localhost");

            Assert.False(result.IsSuccess);
            Assert.Contains("already in use", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ChangeEmailAsync_WhenEmailFree_SendsConfirmationEmail()
        {
            var user = new ApplicationUser { Id = "u1", Email = "old@x.com" };

            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.FindByEmailAsync("new@x.com")).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(m => m.GenerateChangeEmailTokenAsync(user, "new@x.com")).ReturnsAsync("change-token");

            var result = await _sut.ChangeEmailAsync("u1", new EmailDTO { Email = "new@x.com" }, "http://localhost");

            Assert.True(result.IsSuccess);
            _emailMock.Verify(e => e.SendEmailAsync("new@x.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}