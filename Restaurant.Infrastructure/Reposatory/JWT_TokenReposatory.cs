using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Core.DTO.Acoount;
using Restaurant.Core.Models.Account;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Villa_API_Project.DataAccess.Data;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Villa_API_Project.DataAccess.Reposatory
{
    public class JWT_TokenReposatory : IJWT_TokenReposatory
    {
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> userManger;
        private readonly IUnitOfWork unit;
        private readonly Context context;

        public JWT_TokenReposatory(IConfiguration config, UserManager<ApplicationUser> userManger, IUnitOfWork unit, Context context)
        {
            _config = config;
            this.userManger = userManger;
            this.unit = unit;
            this.context = context;
        }

        public async Task<string> GenerateToken(ApplicationUser user, string jwtTokenId)
        {

            var roles = await userManger.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Customer";
            var claims = new[]
            {
               new Claim(ClaimTypes.Name, user.Name.ToString()),
                    new Claim(ClaimTypes.Role,role),
                    new Claim(JwtRegisteredClaimNames.Jti, jwtTokenId),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                       audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }



        public async Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO)
        {
            var existingRefreshToken = unit.RefreshToken.GetByFilter(u => u.Refresh_Token == tokenDTO.RefreshToken);
            if (existingRefreshToken == null)
            {
                return new TokenDTO();
            }

            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid)
            {
                await MarkTokenAsInvalid(existingRefreshToken);
                return new TokenDTO();
            }

            if (!existingRefreshToken.IsValid)
            {
                await MarkAllTokenInChainAsInvalid(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
                return new TokenDTO();
            }
            if (existingRefreshToken.ExpiresAt < DateTime.Now)
            {
                await MarkTokenAsInvalid(existingRefreshToken);
                return new TokenDTO();
            }

            var newRefreshToken = CreateNewRefreshToken(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);


            await MarkTokenAsInvalid(existingRefreshToken);

            // generate new access token
            var applicationUser = unit.User.GetByFilter(u => u.Id == existingRefreshToken.UserId);
            if (applicationUser == null)
                return new TokenDTO();

            var newAccessToken = await GenerateToken(applicationUser, existingRefreshToken.JwtTokenId);

            return new TokenDTO()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
            };

        }

        public async Task RevokeAllTokens(TokenDTO tokenDTO)
        {
            var existingRefreshToken = unit.RefreshToken.GetByFilter(r => r.Refresh_Token == tokenDTO.RefreshToken);

            if (existingRefreshToken == null)
                return;



            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid)
            {

                return;
            }

            await MarkAllTokenInChainAsInvalid(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            //logout jwt token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenDTO.AccessToken);
            var expiryDate = jwtToken.ValidTo;

            await unit.RevokedTokens.AddRevokedTokenAsync(tokenDTO.AccessToken, expiryDate);

        }

        public string CreateNewRefreshToken(string userId, string tokenId)
        {
            RefreshToken refreshToken = new()
            {
                IsValid = true,
                UserId = userId,
                JwtTokenId = tokenId,
                ExpiresAt = DateTime.Now.AddDays(2),
                Refresh_Token = Guid.NewGuid() + "-" + Guid.NewGuid(),
            };

            unit.RefreshToken.Create(refreshToken);
            unit.save();
            return refreshToken.Refresh_Token;
        }

        private bool GetAccessTokenData(string accessToken, string expectedUserId, string expectedTokenId)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(accessToken);
                var jwtTokenId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Jti).Value;
                var userId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub).Value;
                return userId == expectedUserId && jwtTokenId == expectedTokenId;

            }
            catch
            {
                return false;
            }
        }


        private async Task MarkAllTokenInChainAsInvalid(string userId, string tokenId)
        {
            await context.RefreshTokens.Where(u => u.UserId == userId
               && u.JwtTokenId == tokenId)
                   .ExecuteUpdateAsync(u => u.SetProperty(refreshToken => refreshToken.IsValid, false));

        }


        private Task MarkTokenAsInvalid(RefreshToken refreshToken)
        {
            refreshToken.IsValid = false;
            return context.SaveChangesAsync();
        }


    }
}
