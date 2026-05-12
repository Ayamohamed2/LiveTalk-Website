using Restaurant.Core.Models.Account;

namespace Villa_API_Project.DataAccess.Reposatory.IReposatory
{
    public interface IRevokedTokensReposatory:IReposatory<RevokedTokens>
    {
        Task AddRevokedTokenAsync(string token, DateTime expiryDate);
        Task<bool> IsTokenRevokedAsync(string token);
    }
}
