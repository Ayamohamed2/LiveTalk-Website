using Restaurant.Core.Models.Common;

namespace Restaurant.Core.Models.Account
{
    public class RefreshToken : BaseClass
    {
        public string UserId { get; set; }
        public string JwtTokenId { get; set; }
        public string Refresh_Token { get; set; }
        public bool IsValid { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
