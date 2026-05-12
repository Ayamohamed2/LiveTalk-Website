using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using NEEFRA.Core.DTO.Service;
using Restaurant.Core.DTO.Acoount;
using Restaurant.Core.DTO.Email;
using Restaurant.Core.Helpers.EmailTemplate;
using Restaurant.Core.Interfaces.IService;
using Restaurant.Core.Models.Account;
using System.Security.Claims;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Restaurant.Core.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork unit;
        private readonly IAuthRepository auth;
        private readonly IEmailSender sendEmail;
        private readonly IJWT_TokenReposatory jwt_token;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger<AccountService> logger;

        public AccountService(
            IUnitOfWork unit,
            IAuthRepository auth,
            IEmailSender sendEmail,
            IJWT_TokenReposatory jwt_token,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountService> logger)
        {
            this.unit = unit;
            this.auth = auth;
            this.sendEmail = sendEmail;
            this.jwt_token = jwt_token;
            this.userManager = userManager;
            this.logger = logger;
        }

        public async Task<ServiceResult<string>> RegisterAsync(RegisterDTO model, string baseUrl)
        {
            logger.LogInformation("Register attempt for email: {Email}", model.Email);

            var result = await auth.Register(model,baseUrl);

            if (result.Succeeded)
            {
                logger.LogInformation("User registered successfully: {Email}", model.Email);
                return new() { IsSuccess = true, Message = "User registered successfully. pls confirm email" };
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Register failed for email: {Email}. Errors: {Errors}", model.Email, errors);
            return new() { IsSuccess = false, Message = errors, ErrorType = "BadRequest" };
        }

        public async Task<ServiceResult<object>> LoginAsync(LoginDTO model)
        {
            logger.LogInformation("Login attempt for email: {Email}", model.Email);

            var token = await auth.LoginAsync(model);

            if (token.Message == "null")
            {
                logger.LogWarning("Login failed - invalid credentials for email: {Email}", model.Email);
                return new() { IsSuccess = false, Message = "Invalid username or password.", ErrorType = "Unauthorized" };
            }

            if (token.Message == "EmailNotConfirmed")
            {
                logger.LogWarning("Login failed - email not confirmed for: {Email}", model.Email);
                return new() { IsSuccess = false, Message = "EmailNotConfirmed", ErrorType = "BadRequest" };
            }

            if (token.Message == "LockedOut")
            {
                logger.LogWarning("Login failed - account locked out for: {Email}", model.Email);
                return new() { IsSuccess = false, Message = "Account LockedOut for 1 Minute", ErrorType = "BadRequest" };
            }
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user.TwoFactorEnabled)
            {
                logger.LogInformation("2FA enabled - sending OTP to email: {Email}", model.Email);

                var code = await userManager.GenerateTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider);
                logger.LogInformation("OTO Code : " + code);
                var emailBody = EmailTemplates.OtpCode(code);
                await sendEmail.SendEmailAsync(user.Email, "Your Verification Code", emailBody);

                logger.LogInformation("OTP sent successfully to: {Email}", model.Email);

                return new()
                {
                    IsSuccess = true,
                    Message = $"OTP Sent",

                    Data = new
                    {
                        UserId =user.Id
                    }
               
                    };
            }
            logger.LogInformation("Login successful for email: {Email}", model.Email);
            return new() { IsSuccess = true, Data = token };
        }

        public async Task<ServiceResult<TokenDTO>> RefreshTokenAsync(TokenDTO tokenDTO)
        {
            logger.LogDebug("Token refresh attempt");

            var tokenDTOResponse = await jwt_token.RefreshAccessToken(tokenDTO);

            if (tokenDTOResponse == null || string.IsNullOrEmpty(tokenDTOResponse.AccessToken))
            {
                logger.LogWarning("Token refresh failed - invalid token");
                return new() { IsSuccess = false, Message = "Token Invalid", ErrorType = "BadRequest" };
            }

            logger.LogDebug("Token refreshed successfully");
            return new() { IsSuccess = true, Data = tokenDTOResponse };
        }

        public async Task<ServiceResult<string>> LogoutAsync(TokenDTO tokenDTO)
        {
            logger.LogInformation("Logout - revoking tokens");
            await jwt_token.RevokeAllTokens(tokenDTO);
            logger.LogInformation("Tokens revoked successfully");
            return new() { IsSuccess = true };
        }

        public async Task<ServiceResult<string>> ConfirmEmailAsync(string userId, string token)
        {
            logger.LogInformation("Email confirmation attempt for userId: {UserId}", userId);

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                logger.LogWarning("Email confirmation failed - missing userId or token");
                return new() { IsSuccess = false, Message = "Invalid email confirmation request.", ErrorType = "BadRequest" };
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("Email confirmation failed - user not found: {UserId}", userId);
                return new() { IsSuccess = false, Message = "User not found", ErrorType = "NotFound" };
            }

            var result = await userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                logger.LogInformation("Email confirmed successfully for: {Email}", user.Email);
                return new() { IsSuccess = true, Message = "Email confirmed successfully!" };
            }

            logger.LogWarning("Email confirmation failed for: {Email}", user.Email);
            return new() { IsSuccess = false, Message = "Email confirmation failed.", ErrorType = "BadRequest" };
        }

        public async Task<ServiceResult<string>> SendEmailConfirmationAsync(EmailDTO email, string baseUrl)
        {
            logger.LogInformation("Resend email confirmation for: {Email}", email.Email);

            var user = await userManager.FindByEmailAsync(email.Email);
            if (user == null)
            {
                logger.LogWarning("Resend confirmation failed - email not found: {Email}", email.Email);
                return new() { IsSuccess = false, Message = "Email not Found", ErrorType = "BadRequest" };
            }

            var emailtoken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = $"{baseUrl}/api/v2/account/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(emailtoken)}";
            var emailBody = EmailTemplates.ConfirmEmail(confirmationLink);

            await sendEmail.SendEmailAsync(user.Email, "Resend EmailConfirmation",
                emailBody);

            logger.LogInformation("Confirmation email sent to: {Email}", user.Email);
            return new() { IsSuccess = true, Message = "Email sent successfully please confirm your email" };
        }

        public async Task<ServiceResult<string>> ForgetPasswordAsync(EmailDTO emailDTO, string baseUrl)
        {
            logger.LogInformation("Forget password request for: {Email}", emailDTO.Email);

            var user = await userManager.FindByEmailAsync(emailDTO.Email);
            if (user == null)
            {
                logger.LogWarning("Forget password failed - email not found: {Email}", emailDTO.Email);
                return new() { IsSuccess = false, Message = "Email not found", ErrorType = "NotFound" };
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = System.Web.HttpUtility.UrlEncode(token);
            var resetLink = $"{baseUrl}/api/v2/account/reset-password?email={user.Email}&token={encodedToken}";
            var emailBody = EmailTemplates.ResetPassword(resetLink);
            await sendEmail.SendEmailAsync(user.Email, "Reset Your Password", emailBody);
            

            logger.LogInformation("Password reset link sent to: {Email}", user.Email);
            return new() { IsSuccess = true, Message = "Reset password link has been sent to your email." };
        }

        public async Task<ServiceResult<string>> ResetPasswordAsync(string email, string token, ResetPasswordDTO model)
        {
            logger.LogInformation("Reset password attempt for: {Email}", email);

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                logger.LogWarning("Reset password failed - email not found: {Email}", email);
                return new() { IsSuccess = false, Message = "Invalid Email", ErrorType = "BadRequest" };
            }

            var decodedToken = System.Web.HttpUtility.UrlDecode(token);
            var result = await userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

            if (result.Succeeded)
            {
                logger.LogInformation("Password reset successfully for: {Email}", email);
                return new() { IsSuccess = true, Message = "Password has been reset successfully." };
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Reset password failed for: {Email}. Errors: {Errors}", email, errors);
            return new() { IsSuccess = false, Message = errors, ErrorType = "BadRequest" };
        }

        public async Task<ServiceResult<object>> ExternalLoginCallbackAsync(string provider, HttpContext httpContext)
        {
            logger.LogInformation("External login callback for provider: {Provider}", provider);

            var result = await httpContext.AuthenticateAsync("ExternalCookies");
            if (!result.Succeeded)
            {
                logger.LogWarning("External authentication failed for provider: {Provider}", provider);
                return new() { IsSuccess = false, Message = "External authentication failed", ErrorType = "BadRequest" };
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var providerKey = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                logger.LogWarning("External login failed - no email provided by provider: {Provider}", provider);
                return new() { IsSuccess = false, Message = "Email not provided by provider", ErrorType = "BadRequest" };
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                logger.LogInformation("Creating new user from external login: {Email}", email);

                user = new ApplicationUser { UserName = email, Email = email, Name = name, EmailConfirmed = true };
                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    logger.LogWarning("External login - user creation failed for: {Email}. Errors: {Errors}", email, errors);
                    return new() { IsSuccess = false, Message = errors, ErrorType = "BadRequest" };
                }

                var addLoginResult = await userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
                if (!addLoginResult.Succeeded)
                {
                    logger.LogWarning("External login - failed to link provider {Provider} for: {Email}", provider, email);
                    return new() { IsSuccess = false, Message = "Failed to link external login", ErrorType = "BadRequest" };
                }
            }
            else
            {
                var logins = await userManager.GetLoginsAsync(user);
                var isLinked = logins.Any(l => l.LoginProvider == provider && l.ProviderKey == providerKey);
                if (!isLinked)
                {
                    logger.LogWarning("External login - email {Email} already registered with another method", email);
                    return new() { IsSuccess = false, Message = "This email is already registered with another method.", ErrorType = "BadRequest" };
                }
            }

            string jwtTokenId = $"JTI{Guid.NewGuid()}";
            var accessToken = await jwt_token.GenerateToken(user, jwtTokenId);
            var refreshToken = jwt_token.CreateNewRefreshToken(user.Id, jwtTokenId);

            logger.LogInformation("External login successful for: {Email} via {Provider}", email, provider);
            return new() { IsSuccess = true, Data = (object)new { email, accessToken, refreshToken } };
        }

        public async Task<ServiceResult<string>> ChangeEmailAsync(string userId, EmailDTO model, string baseUrl)
        {
            logger.LogInformation("Change email request for userId: {UserId}", userId);

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("Change email failed - user not found: {UserId}", userId);
                return new() { IsSuccess = false, Message = "User not found", ErrorType = "BadRequest" };
            }

            var existingUser = await userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                logger.LogWarning("Change email failed - email already in use: {Email}", model.Email);
                return new() { IsSuccess = false, Message = "Email already in use", ErrorType = "BadRequest" };
            }

            var token = await userManager.GenerateChangeEmailTokenAsync(user, model.Email);
            var confirmationLink = $"{baseUrl}/api/v2/account/confirm-change-email?userId={user.Id}&newEmail={model.Email}&token={Uri.EscapeDataString(token)}";
            var emailBody = EmailTemplates.ConfirmEmail(confirmationLink);

            
            await sendEmail.SendEmailAsync(model.Email, "Confirm your new email",
              emailBody);

            logger.LogInformation("Change email confirmation sent to: {NewEmail} for userId: {UserId}", model.Email, userId);
            return new() { IsSuccess = true, Message = "Confirmation email has been sent to your new address." };
        }

        public async Task<ServiceResult<string>> ConfirmChangeEmailAsync(string userId, string newEmail, string token)
        {
            logger.LogInformation("Confirm change email for userId: {UserId}, newEmail: {NewEmail}", userId, newEmail);

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("Confirm change email failed - user not found: {UserId}", userId);
                return new() { IsSuccess = false, Message = "User not found", ErrorType = "BadRequest" };
            }

            var decodedToken = Uri.UnescapeDataString(token);
            var result = await userManager.ChangeEmailAsync(user, newEmail, decodedToken);
            if (!result.Succeeded)
            {
                logger.LogWarning("Confirm change email failed for userId: {UserId}", userId);
                return new() { IsSuccess = false, Message = "Failed to change email", ErrorType = "BadRequest" };
            }

            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);

            if (user.UserName != null && user.UserName.Contains("@"))
                await userManager.SetUserNameAsync(user, newEmail);

            logger.LogInformation("Email changed successfully for userId: {UserId} to: {NewEmail}", userId, newEmail);
            return new() { IsSuccess = true, Message = "Email changed and confirmed successfully." };
        }

        public async Task<ServiceResult<string>> ChangePasswordAsync(string userId, ChangePasswordDTO model)
        {
            logger.LogInformation("Change password request for userId: {UserId}", userId);

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("Change password failed - user not found: {UserId}", userId);
                return new() { IsSuccess = false, Message = "User not found", ErrorType = "BadRequest" };
            }

            var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("\n", result.Errors.Select(e => e.Description));
                logger.LogWarning("Change password failed for userId: {UserId}. Errors: {Errors}", userId, errors);
                return new() { IsSuccess = false, Message = errors, ErrorType = "BadRequest" };
            }

            logger.LogInformation("Password changed successfully for userId: {UserId}", userId);
            return new() { IsSuccess = true, Message = "Password changed successfully." };
        }


        public async Task<ServiceResult<TokenDTO>> VerifyTwoFactorAsync(string userId, string code)
        {
            logger.LogInformation("Verify 2FA attempt for userId: {UserId}", userId);

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                logger.LogWarning("Verify 2FA failed - user not found: {UserId}", userId);
                return new() { IsSuccess = false, Message = "User not found" };
            }

            var isValid = await userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultEmailProvider,
                code);

            if (!isValid)
            {
                logger.LogWarning("Verify 2FA failed - invalid code for userId: {UserId}", userId);
                return new() { IsSuccess = false, Message = "Invalid Code" };
            }

            logger.LogInformation("2FA verified successfully for userId: {UserId}", userId);

            string jwtTokenId = $"JTI{Guid.NewGuid()}";

            var accessToken = await jwt_token.GenerateToken(user, jwtTokenId);
            var refreshToken = jwt_token.CreateNewRefreshToken(user.Id, jwtTokenId);

            return new()
            {
                IsSuccess = true,
                Data = new TokenDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                }
            };
        }


        public async Task<ServiceResult<string>> EnableTwoFactorAsync(string userId)
        {
            logger.LogInformation("Enable 2FA request for userId: {UserId}", userId);

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                logger.LogWarning("Enable 2FA failed - user not found: {UserId}", userId);
                return new() { IsSuccess = false, Message = "User not found" };
            }

            user.TwoFactorEnabled = true;

            await userManager.UpdateAsync(user);

            logger.LogInformation("2FA enabled successfully for userId: {UserId}", userId);

            return new()
            {
                IsSuccess = true,
                Message = "Two Factor Enabled Successfully"
            };
        }


        public async Task<ServiceResult<string>> DisableTwoFactorAsync(string userId)
        {
            logger.LogInformation("Disable 2FA request for userId: {UserId}", userId);

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                logger.LogWarning("Disable 2FA failed - user not found: {UserId}", userId);
                return new() { IsSuccess = false, Message = "User not found" };
            }

            user.TwoFactorEnabled = false;

            await userManager.UpdateAsync(user);

            logger.LogInformation("2FA disabled successfully for userId: {UserId}", userId);

            return new()
            {
                IsSuccess = true,
                Message = "Two Factor Disabled Successfully"
            };
        }
    }
}