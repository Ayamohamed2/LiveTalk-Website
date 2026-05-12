using Microsoft.EntityFrameworkCore;
using Restaurant.Core.Models.Account;
using Villa_API_Project.DataAccess.Data;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Villa_API_Project.DataAccess.Reposatory
{
    public class RefreshTokenReposatory:Reposatory<RefreshToken>,IRefreshTokenReposatory
    {
        Context Context;
        public RefreshTokenReposatory(Context context) : base(context)
        {
            this.Context = context;

        }

       
    }
}
