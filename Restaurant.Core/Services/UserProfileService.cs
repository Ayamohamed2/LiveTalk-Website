using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NEEFRA.Core.DTO.Service;
using Restaurant.Core.DTO.Profie;
using Restaurant.Core.Interfaces.IService;
using Restaurant.Core.Interfaces.IService.Redis;
using Restaurant.Core.Models.Account;
using StackExchange.Redis;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Restaurant.Core.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUnitOfWork unit;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger<UserProfileService> logger;
        private readonly IRedisCacheService cache;

        public UserProfileService(IUnitOfWork unit, UserManager<ApplicationUser> userManager, ILogger<UserProfileService> logger,IRedisCacheService cache)
        {
            this.unit = unit;
            this.userManager = userManager;
            this.logger = logger;
            this.cache = cache;
        }

        public async Task<ServiceResult<object>> GetProfileAsync(string userId, string baseUrl)
        {
            logger.LogInformation("Get profile for userId: {UserId}", userId);
            var cachekey = $"Profile:{userId}";
            ApplicationUser user =await cache.GetAsync<ApplicationUser>(cachekey);
            if (user == null)
            {
            user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    logger.LogWarning("Get profile failed - user not found: {UserId}", userId);
                    return new() { IsSuccess = false, Message = "User not found", ErrorType = "BadRequest" };
                }
                var profile = new
                {
                    user.Email,
                    user.Name,
                    user.PhoneNumber,
                    user.ImageURL
                };
                await cache.SetAsync(cachekey, profile, TimeSpan.FromMinutes(30));


            }
          
            if (user == null)
            {
                logger.LogWarning("Get profile failed - user not found: {UserId}", userId);
                return new() { IsSuccess = false, Message = "User not found", ErrorType = "BadRequest" };
            }

            var imageUrl = string.IsNullOrEmpty(user.ImageURL) ? null : baseUrl + user.ImageURL;
            

            logger.LogDebug("Profile retrieved successfully for userId: {UserId}", userId);
            return new()
            {
                IsSuccess = true,
                Data = (object)new
                {
                    user.Email,
                    user.Name,
                    user.PhoneNumber,
                    imageUrl
                }
            };
        }

        public async Task<ServiceResult<object>> UpdateProfileAsync(string userId, UserProfileDTO profileDTO, string baseUrl, IWebHostEnvironment env)
        {
            logger.LogInformation("Update profile for userId: {UserId}", userId);

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("Update profile failed - user not found: {UserId}", userId);
                return new() { IsSuccess = false, Message = "User not found", ErrorType = "BadRequest" };
            }

            if (profileDTO.Name != null)
                user.Name = profileDTO.Name;

            user.PhoneNumber = profileDTO.phoneNumber;

            if (profileDTO.imagefile != null && profileDTO.imagefile.Length > 0)
            {
                logger.LogDebug("Updating profile image for userId: {UserId}", userId);
                if (user.ImageURL != "/Images/default.png")
                    unit.User.DeleteImageMethod(user.ImageURL, env);
                user.ImageURL = unit.User.GetImageURL(profileDTO.imagefile, user.Id, env);
            }

            await userManager.UpdateAsync(user);

            var imageUrl = string.IsNullOrEmpty(user.ImageURL) ? null : baseUrl + user.ImageURL;

            logger.LogInformation("Profile updated successfully for userId: {UserId}", userId);
          
            await cache.RemoveAsync($"Profile:{userId}");
            return new()
            {
                IsSuccess = true,
                Data = (object)new
                {
                    user.Email,
                    user.Name,
                    user.PhoneNumber,
                    imageUrl
                }
            };
        }
    }
}