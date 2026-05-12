using Microsoft.AspNetCore.Hosting;
using NEEFRA.Core.DTO.Service;
using Restaurant.Core.DTO.Profie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.Interfaces.IService
{
    public interface IUserProfileService
    {
        Task<ServiceResult<object>> GetProfileAsync(string userId, string baseUrl);
        Task<ServiceResult<object>> UpdateProfileAsync(string userId, UserProfileDTO profileDTO, string baseUrl, IWebHostEnvironment env);
    }
}
