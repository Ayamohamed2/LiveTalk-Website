using Realtima_Chat_project.DataAccess.Reposatory.IReposatory;

namespace Villa_API_Project.DataAccess.Reposatory.IReposatory
{
    public interface IUnitOfWork
    {
   

        public IAPPlicationUserReposatory User { get; }
        public IRefreshTokenReposatory RefreshToken { get;  }
        public IRevokedTokensReposatory RevokedTokens { get; }
        public IUserConReposatory UserCon { get; }
        public IMessageReposatory Message { get; }
        public ICallReposatory Call { get; }

        public IGroupReposatory Group { get; }
        public IGroupMemberReposatory GroupMember { get; }
        public IGroupMessagesReposatory GroupMessages { get; }
        public IGroupMessageReadRepo GroupMessageRead { get; }

        public IGroupMessageDeletedRepo GroupMessageDeleted { get; }
        public IMessageDeletedRepo MessageDeleted { get; }
        public IBlockprepo BlockedUsers { get; }

        void save();
    }
}
