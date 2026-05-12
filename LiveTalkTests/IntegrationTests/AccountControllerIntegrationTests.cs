using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.TestHelpers;
using Moq;
using NEEFRA.Core.DTO.Service;
using Restaurant.Core.DTO.Acoount;
using Restaurant.Core.DTO.Email;
using Xunit;

namespace IntegrationTests
{
    public class AccountControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public AccountControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private HttpClient AuthClient()
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            return c;
        }

      
     

        [Fact]
        public async Task Register_DuplicateEmail_Returns400()
        {
            _factory.AccountServiceMock
                .Setup(s => s.RegisterAsync(It.IsAny<RegisterDTO>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Fail("Email already exists"));

            var response = await _client.PostAsJsonAsync("/api/v2/Account/register",
                new { Email = "duplicate@test.com", Password = "Pass@1234" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

       

        [Fact]
        public async Task Login_ValidCredentials_Returns200WithToken()
        {
            _factory.AccountServiceMock
                .Setup(s => s.LoginAsync(It.IsAny<LoginDTO>()))
                .ReturnsAsync(ServiceResult<object>.Ok("jwt_token_here"));

            var response = await _client.PostAsJsonAsync("/api/v2/Account/login",
                new { Email = "user@test.com", Password = "Pass@1234" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("jwt_token_here");
        }

        [Fact]
        public async Task Login_WrongCredentials_Returns401()
        {
            _factory.AccountServiceMock
                .Setup(s => s.LoginAsync(It.IsAny<LoginDTO>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Invalid credentials", "Unauthorized"));

            var response = await _client.PostAsJsonAsync("/api/v2/Account/login",
                new { Email = "user@test.com", Password = "wrong" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_RequiresTwoFactor_Returns200WithTwoFactorFlag()
        {
            _factory.AccountServiceMock
                .Setup(s => s.LoginAsync(It.IsAny<LoginDTO>()))
                .ReturnsAsync(ServiceResult<object>.Ok("2fa_required"));

            var response = await _client.PostAsJsonAsync("/api/v2/Account/login",
                new { Email = "2fa@test.com", Password = "Pass@1234" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

    

        [Fact]
        public async Task Refresh_ExpiredToken_Returns400()
        {
            _factory.AccountServiceMock
                .Setup(s => s.RefreshTokenAsync(It.IsAny<TokenDTO>()))
                .ReturnsAsync(ServiceResult<TokenDTO>.Fail("Refresh token expired"));

            var response = await _client.PostAsJsonAsync("/api/v2/Account/refresh",
                new { AccessToken = "old", RefreshToken = "expired" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        

        [Fact]
        public async Task Logout_ValidToken_Returns200()
        {
            _factory.AccountServiceMock
                .Setup(s => s.LogoutAsync(It.IsAny<TokenDTO>()))
                .ReturnsAsync(ServiceResult<string>.Ok("Logged out"));

            var response = await AuthClient().PostAsJsonAsync("/api/v2/Account/logout",
                new { AccessToken = "token", RefreshToken = "refresh" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

       

        [Fact]
        public async Task ConfirmEmail_ValidToken_Returns200()
        {
            _factory.AccountServiceMock
                .Setup(s => s.ConfirmEmailAsync("user-123", "valid-token"))
                .ReturnsAsync(ServiceResult<string>.Ok("Email confirmed"));

            var response = await _client.GetAsync("/api/v2/Account/confirm-email?userId=user-123&token=valid-token");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ConfirmEmail_InvalidToken_Returns400()
        {
            _factory.AccountServiceMock
                .Setup(s => s.ConfirmEmailAsync(It.IsAny<string>(), "bad-token"))
                .ReturnsAsync(ServiceResult<string>.Fail("Invalid or expired token"));

            var response = await _client.GetAsync("/api/v2/Account/confirm-email?userId=user-123&token=bad-token");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }



        [Fact]
        public async Task ForgetPassword_ExistingEmail_Returns200()
        {
            _factory.AccountServiceMock
                .Setup(s => s.ForgetPasswordAsync(It.IsAny<EmailDTO>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Ok("Reset link sent"));

            var response = await _client.PostAsJsonAsync("/api/v2/Account/forget-password",
                new { Email = "user@test.com" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ForgetPassword_NonExistingEmail_Returns404()
        {
            _factory.AccountServiceMock
                .Setup(s => s.ForgetPasswordAsync(It.IsAny<EmailDTO>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Fail("Email not found", "NotFound"));

            var response = await _client.PostAsJsonAsync("/api/v2/Account/forget-password",
                new { Email = "ghost@test.com" });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Fact]
        public async Task ResetPassword_ValidData_Returns200()
        {
            _factory.AccountServiceMock
                .Setup(s => s.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ResetPasswordDTO>()))
                .ReturnsAsync(ServiceResult<string>.Ok("Password reset successfully"));

            var response = await _client.PostAsJsonAsync(
                "/api/v2/Account/reset-password?email=user@test.com&token=valid-token",
                new { NewPassword = "New@Pass123", ConfirmPassword = "New@Pass123" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ResetPassword_InvalidToken_Returns400()
        {
            _factory.AccountServiceMock
                .Setup(s => s.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ResetPasswordDTO>()))
                .ReturnsAsync(ServiceResult<string>.Fail("Invalid token"));

            var response = await _client.PostAsJsonAsync(
                "/api/v2/Account/reset-password?email=user@test.com&token=bad",
                new { NewPassword = "New@Pass123", ConfirmPassword = "New@Pass123" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

    

        [Fact]
        public async Task ChangeEmail_Authenticated_Returns200()
        {
            _factory.AccountServiceMock
                .Setup(s => s.ChangeEmailAsync(It.IsAny<string>(), It.IsAny<EmailDTO>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Ok("Confirmation email sent"));

            var response = await AuthClient().PostAsJsonAsync("/api/v2/Account/change-email",
                new { Email = "newemail@test.com" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ChangeEmail_Unauthenticated_Returns401()
        {
            var response = await _client.PostAsJsonAsync("/api/v2/Account/change-email",
                new { Email = "newemail@test.com" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }





        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_Returns400()
        {
            _factory.AccountServiceMock
                .Setup(s => s.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<ChangePasswordDTO>()))
                .ReturnsAsync(ServiceResult<string>.Fail("Incorrect current password"));

            var response = await AuthClient().PostAsJsonAsync("/api/v2/Account/change-password",
                new { CurrentPassword = "Wrong", NewPassword = "New@123", ConfirmPassword = "New@123" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ChangePassword_Unauthenticated_Returns401()
        {
            var response = await _client.PostAsJsonAsync("/api/v2/Account/change-password",
                new { CurrentPassword = "Old@123", NewPassword = "New@123" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

   

        [Fact]
        public async Task Verify2FA_ValidCode_Returns200()
        {
            _factory.AccountServiceMock
                .Setup(s => s.VerifyTwoFactorAsync("user-123", "123456"))
                .ReturnsAsync(ServiceResult<TokenDTO>.Ok(new TokenDTO { Message = "Verified" }));

            var response = await _client.PostAsync(
                "/api/v2/Account/verify-2fa?Code=123456&userId=user-123", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Verify2FA_InvalidCode_Returns400()
        {
            _factory.AccountServiceMock
                .Setup(s => s.VerifyTwoFactorAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<TokenDTO>.Fail("Invalid 2FA code"));

            var response = await _client.PostAsync(
                "/api/v2/Account/verify-2fa?Code=000000&userId=user-123", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Enable2FA_Authenticated_Returns200()
        {
            _factory.AccountServiceMock
                .Setup(s => s.EnableTwoFactorAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Ok("2FA enabled"));

            var response = await AuthClient().PostAsync("/api/v2/Account/enable-2fa", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Enable2FA_Unauthenticated_Returns401()
        {
            var response = await _client.PostAsync("/api/v2/Account/enable-2fa", null);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Disable2FA_Authenticated_Returns200()
        {
            _factory.AccountServiceMock
                .Setup(s => s.DisableTwoFactorAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Ok("2FA disabled"));

            var response = await AuthClient().PostAsync("/api/v2/Account/disable-2fa", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

    }
}
