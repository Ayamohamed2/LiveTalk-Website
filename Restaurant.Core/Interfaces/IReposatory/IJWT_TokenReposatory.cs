using Restaurant.Core.DTO.Acoount;
using Restaurant.Core.Models.Account;

namespace Villa_API_Project.DataAccess.Reposatory.IReposatory
{
    public interface IJWT_TokenReposatory
    {
        Task<string> GenerateToken(ApplicationUser user, string jwtTokenId);
        Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO);

        Task RevokeAllTokens(TokenDTO tokenDTO);
        public string CreateNewRefreshToken(string userId, string tokenId);

    }
}
