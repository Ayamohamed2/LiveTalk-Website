using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Core.DTO.Acoount;
using Restaurant.Core.Helpers.EmailTemplate;
using Restaurant.Core.Models.Account;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;

using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Villa_API_Project.DataAccess.Reposatory
{
    public class AuthRepository:IAuthRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        RoleManager<IdentityRole> roleManager;
        SignInManager<ApplicationUser> _signinManager;

        IJWT_TokenReposatory Token;
        private readonly IEmailSender sendEmail;
        public AuthRepository(IEmailSender sendEmail,IJWT_TokenReposatory Token, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> _signinManager)
        {
            _userManager = userManager;
            this.roleManager = roleManager;
            this._signinManager = _signinManager;
            this.Token = Token;
            this.sendEmail = sendEmail;
        }

        public async Task<TokenDTO> LoginAsync(LoginDTO model)
        {


            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)return new TokenDTO { Message = "null" };
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return new TokenDTO { Message = "EmailNotConfirmed" }; 
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return new TokenDTO { Message = "LockedOut" };
            }

            var result = await _signinManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return new TokenDTO { Message = "LockedOut" };
            }
            if (!result.Succeeded)
            {
                return  new TokenDTO { Message = "null" };
            }

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            string jwtTokenId = $"JTI{Guid.NewGuid()}";
            var accessToken = await Token.GenerateToken(user,jwtTokenId);
            var refreshToken =   Token.CreateNewRefreshToken(user.Id, jwtTokenId);

            return   new TokenDTO { Message = "Login succsee",AccessToken=accessToken,RefreshToken=refreshToken };

        
        }

        public  async Task<IdentityResult>Register(RegisterDTO model,string baseurl)
        {
            


            var byEmail =await  _userManager.FindByEmailAsync(model.Email);
            if (byEmail != null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = "Email already exists."
                });
            }


            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name
            };


            var result = await _userManager.CreateAsync(user, model.Password);


            if (result.Succeeded)
            {
            if(! await roleManager.RoleExistsAsync("Customer"))
                {
                   await roleManager.CreateAsync(new IdentityRole("Customer"));
                }
                await _userManager.AddToRoleAsync(user, "Customer");
                var emailtoken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = $"{baseurl}/api/v2/account/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(emailtoken)}";
                var emailBody = EmailTemplates.ConfirmEmail(confirmationLink);
                await sendEmail.SendEmailAsync(model.Email, "Confirm Your Email", emailBody);
                return result;

            }


            return result;
        }

        
    }
}
