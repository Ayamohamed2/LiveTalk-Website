using Microsoft.AspNetCore.Http;
using NEEFRA.Core.DTO.Service;
using Restaurant.Core.DTO.Acoount;
using Restaurant.Core.DTO.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.Interfaces.IService
{
    public interface IAccountService
    {
        Task<ServiceResult<string>> RegisterAsync(RegisterDTO model, string baseUrl);
        Task<ServiceResult<object>> LoginAsync(LoginDTO model);
        Task<ServiceResult<TokenDTO>> RefreshTokenAsync(TokenDTO dto);
        Task<ServiceResult<string>> LogoutAsync(TokenDTO dto);
        Task<ServiceResult<string>> ConfirmEmailAsync(string userId, string token);
        Task<ServiceResult<string>> SendEmailConfirmationAsync(EmailDTO email, string baseUrl);
        Task<ServiceResult<string>> ForgetPasswordAsync(EmailDTO model, string baseUrl);
        Task<ServiceResult<string>> ResetPasswordAsync(string email, string token, ResetPasswordDTO model);
        Task<ServiceResult<object>> ExternalLoginCallbackAsync(string provider, HttpContext httpContext);
        Task<ServiceResult<string>> ChangeEmailAsync(string userId, EmailDTO model, string baseUrl);
        Task<ServiceResult<string>> ConfirmChangeEmailAsync(string userId, string newEmail, string token);
        Task<ServiceResult<string>> ChangePasswordAsync(string userId, ChangePasswordDTO model);

        Task<ServiceResult<TokenDTO>> VerifyTwoFactorAsync(string userId, string code);

        Task<ServiceResult<string>> EnableTwoFactorAsync(string userId);

        Task<ServiceResult<string>> DisableTwoFactorAsync(string userId);
    }
}
