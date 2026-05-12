using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NEEFRA.API.Helpers;
using NEEFRA.Core.DTO.Service;
using Restaurant.API.Controllers;
using Restaurant.Core.DTO.Profie;
using Restaurant.Core.Interfaces.IService;
using Restaurant.Core.Models.Account;
using System.Security.Claims;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Villa_API_Project.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class UserProfileController : BaseController
    {
        private readonly IUserProfileService userProfileService;

        public UserProfileController(IUserProfileService userProfileService)
        {
            this.userProfileService = userProfileService;
        }

      

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var result = await userProfileService.GetProfileAsync(UserId, BaseUrl);
            return HandleResult(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromForm] UserProfileDTO profileDTO)
        {
            var result = await userProfileService.UpdateProfileAsync(UserId, profileDTO, BaseUrl, HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>());
            return HandleResult(result);
        }
    }
}

