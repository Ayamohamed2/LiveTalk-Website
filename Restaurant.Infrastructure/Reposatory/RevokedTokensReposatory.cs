using Microsoft.EntityFrameworkCore;
using Restaurant.Core.Models.Account;
using Villa_API_Project.DataAccess.Data;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Villa_API_Project.DataAccess.Reposatory
{
    public class RevokedTokensReposatory:Reposatory<RevokedTokens>,IRevokedTokensReposatory
    {

        Context Context;
        public RevokedTokensReposatory(Context context) : base(context)
        {
            this.Context = context;

        }
        public async Task AddRevokedTokenAsync(string token, DateTime expiryDate)
        {
            var revoked = new RevokedTokens
            {
                Token = token,
                ExpiredAt = expiryDate
            };

            Create(revoked);
            await Context.SaveChangesAsync();
        }

        public async Task<bool> IsTokenRevokedAsync(string token)
        {
            return await Context.RevokedTokens
                       .AnyAsync(t => t.Token == token);
        }
    }
}
