using Realtima_Chat_project.DataAccess.Reposatory.IReposatory;
using Realtima_Chat_project.Models;
using Villa_API_Project.DataAccess.Data;
using Villa_API_Project.DataAccess.Reposatory;

namespace Restaurant.Infrastructure.Reposatory
{
    public class GRoupMessageRead : Reposatory<GroupMessageRead>, IGroupMessageReadRepo
    {
        Context Context;
        public GRoupMessageRead(Context context) : base(context)
        {
            Context = context;

        }
    }
}
