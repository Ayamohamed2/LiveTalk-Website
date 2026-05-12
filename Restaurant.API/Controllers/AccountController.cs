using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using NEEFRA.API.Helpers;
using NEEFRA.Core.DTO.Service;
using Restaurant.API.Controllers;
using Restaurant.Core.DTO.Acoount;
using Restaurant.Core.DTO.Email;
using Restaurant.Core.Interfaces.IService;
using Restaurant.Core.Models.Account;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;
using Villa_API_Project.Models;

namespace Villa_API_Project.Controllers.V2
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]

    public class AccountController : BaseController
    {
        private readonly IAccountService accountService;

        public AccountController(IAccountService accountService)
        {
            this.accountService = accountService;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO model)
        {
            var result = await accountService.RegisterAsync(model, BaseUrl);
            return HandleResult(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO model)
        {
            var result = await accountService.LoginAsync(model);
            return HandleResult(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenDTO dto)
        {
            var result = await accountService.RefreshTokenAsync(dto);
            return HandleResult(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] TokenDTO dto)
        {
            var result = await accountService.LogoutAsync(dto);
            return HandleResult(result);
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var result = await accountService.ConfirmEmailAsync(userId, token);
            return HandleResult(result);
        }

        [HttpPost("email-confirmation")]
        public async Task<IActionResult> EmailForConfirmation(EmailDTO email)
        {
            var result = await accountService.SendEmailConfirmationAsync(email, BaseUrl);
            return HandleResult(result);
        }

        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword(EmailDTO model)
        {
            var result = await accountService.ForgetPasswordAsync(model, BaseUrl);
            return HandleResult(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(string email, string token, ResetPasswordDTO model)
        {
            var result = await accountService.ResetPasswordAsync(email, token, model);
            return HandleResult(result);
        }

        [HttpGet("external-login")]
        public IActionResult ExternalLogin(string provider)
        {
            var redirectUrl = $"{Request.Scheme}://{Request.Host}/api/v2/Account/external-login-callback?provider={provider}";
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet("external-login-callback")]
        public async Task<IActionResult> ExternalLoginCallback(string provider)
        {
            var result = await accountService.ExternalLoginCallbackAsync(provider, HttpContext);
            return HandleResult(result);
        }

        [Authorize]
        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeEmail(EmailDTO model)
        {
            var result = await accountService.ChangeEmailAsync(UserId, model, BaseUrl);
            return HandleResult(result);
        }

        [HttpGet("confirm-change-email")]
        public async Task<IActionResult> ConfirmChangeEmail(string userId, string newEmail, string token)
        {
            var result = await accountService.ConfirmChangeEmailAsync(userId, newEmail, token);
            return HandleResult(result);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO model)
        {
            var result = await accountService.ChangePasswordAsync(UserId, model);
            return HandleResult(result);
        }
        [HttpPost("verify-2fa")]
        public async Task<IActionResult> Verify2FA(string Code,string userId)
        {
            var result = await accountService.VerifyTwoFactorAsync(userId, Code);


            return HandleResult(result);
        }


        [Authorize]
        [HttpPost("enable-2fa")]
        public async Task<IActionResult> Enable2FA()
        {

            var result = await accountService.EnableTwoFactorAsync(UserId);


            return HandleResult(result);
        }


        [Authorize]
        [HttpPost("disable-2fa")]
        public async Task<IActionResult> Disable2FA()
        {

            var result = await accountService.DisableTwoFactorAsync(UserId);



            return HandleResult(result);
        }

    }
}
