using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Restaurant.Core.Models.Account;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Restaurant.API.Background_Services
{
    public class ExpiredTokensCleanupService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ExpiredTokensCleanupService> _logger;
        private readonly IUnitOfWork unit;

        public ExpiredTokensCleanupService(
            UserManager<ApplicationUser> userManager,
            ILogger<ExpiredTokensCleanupService> logger,IUnitOfWork unit)
        {
            _userManager = userManager;
            _logger = logger;
            this.unit = unit;
        }

        public async Task CleanupAsync()
        {
            _logger.LogInformation("Starting token cleanup...");

            var tokens =  unit.RefreshToken.GetALL(t => t.ExpiresAt < DateTime.UtcNow);
             unit.RefreshToken.RemoveRange(tokens);
            unit.save();


      
        }
    }
}
