using Microsoft.AspNetCore.Identity;
using Restaurant.Core.DTO.Acoount;

namespace Villa_API_Project.DataAccess.Reposatory.IReposatory
{
    public interface IAuthRepository
    {
        Task<IdentityResult> Register(RegisterDTO model, string baseurl);

        Task<TokenDTO> LoginAsync(LoginDTO model);
    }
}
