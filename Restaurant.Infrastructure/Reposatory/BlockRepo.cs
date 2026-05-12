using Realtima_Chat_project.DataAccess.Reposatory.IReposatory;
using Realtima_Chat_project.Models;
using Restaurant.Core.Entity.Chat;
using Villa_API_Project.DataAccess.Data;
using Villa_API_Project.DataAccess.Reposatory;

namespace Restaurant.Infrastructure.Reposatory
{
    public class BlockRepo : Reposatory<BlockedUser>, IBlockprepo
    {
        Context Context;
        public BlockRepo(Context context) : base(context)
        {
            Context = context;

        }
    }
}
