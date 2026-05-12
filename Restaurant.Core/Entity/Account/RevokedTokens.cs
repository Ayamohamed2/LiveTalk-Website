using Restaurant.Core.Models.Common;

namespace Restaurant.Core.Models.Account
{
    public class RevokedTokens : BaseClass
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiredAt { get; set; }
    }
}
