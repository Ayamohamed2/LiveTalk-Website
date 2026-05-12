using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Villa_API_Project.DataAccess.Reposatory.IReposatory
{
    public interface IImageRepo
    {
        void DeleteImageMethod(string imageURL, IWebHostEnvironment env);
        string GetImageURL(IFormFile ImageFile, string id, IWebHostEnvironment env);
    }
}
