using Realtima_Chat_project.DataAccess.Reposatory.IReposatory;
using Realtima_Chat_project.Models;
using Villa_API_Project.DataAccess.Data;
using Villa_API_Project.DataAccess.Reposatory;

namespace Restaurant.Infrastructure.Reposatory
{
    public class GroupMemberRepo : Reposatory<GroupMember>, IGroupMemberReposatory
    {
        Context Context;
        public GroupMemberRepo(Context context) : base(context)
        {
            Context = context;

        }
    }
}
