using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Realtima_Chat_project.DataAccess.Reposatory;
using Realtima_Chat_project.DataAccess.Reposatory.IReposatory;
using Restaurant.Core.Models.Account;
using Villa_API_Project.DataAccess.Data;
using Villa_API_Project.DataAccess.Reposatory;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Restaurant.Infrastructure.Reposatory
{
    public class UnitOfWork : IUnitOfWork
    {


        public IAPPlicationUserReposatory User { get; private set; }

        public IRefreshTokenReposatory RefreshToken { get; private set; }

        public IRevokedTokensReposatory RevokedTokens { get; private set; }

        public IUserConReposatory UserCon { get; private set; }

        public IMessageReposatory Message { get; private set; }

        public ICallReposatory Call { get; private set; }

        public IGroupReposatory Group { get; private set; }

        public IGroupMemberReposatory GroupMember { get; private set; }

        public IGroupMessagesReposatory GroupMessages { get; private set; }

        public IGroupMessageReadRepo GroupMessageRead { get; private set; }

        public IGroupMessageDeletedRepo GroupMessageDeleted { get; private set; }

        public IMessageDeletedRepo MessageDeleted { get; private set; }

        public IBlockprepo BlockedUsers { get; private set; }

        private Context context;
        private UserManager<ApplicationUser> userManager;
        private readonly IConfiguration _config;

        public UnitOfWork(Context context)
        {
            this.context = context;


            User = new ApplicationUserReposatory(context);
            RefreshToken = new RefreshTokenReposatory(context);
            UserCon = new UserConReposatory(context);
            RevokedTokens = new RevokedTokensReposatory(context);

            Message = new MessageReposatory(context);
            Call = new CallReposatory(context);

            Group = new GroupRepo(context);

            GroupMember = new GroupMemberRepo(context);

            GroupMessages = new GroupMessagesRepo(context);

            GroupMessageRead = new GRoupMessageRead(context);

            GroupMessageDeleted = new GroupMessageDeletedRepo(context);
            MessageDeleted = new MessageDeletedRepo(context);
            BlockedUsers = new BlockRepo(context);

        }
        public void save()
        {
            context.SaveChanges();
        }
    }
}
