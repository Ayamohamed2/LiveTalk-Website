using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Realtima_Chat_project.Models;
using Restaurant.Core.Entity.Chat;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Realtima_Chat_project.DataAccess.Reposatory.IReposatory
{
    public interface IMessageReposatory:IReposatory<Message>
    {
        void DeleteImageMethod(string file, IWebHostEnvironment env);
        string GetImageURL(IFormFile file, IWebHostEnvironment env, MessageType Type);
    }
}
