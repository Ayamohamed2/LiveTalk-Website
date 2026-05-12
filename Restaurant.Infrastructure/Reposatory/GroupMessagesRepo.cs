using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Realtima_Chat_project.DataAccess.Reposatory.IReposatory;
using Realtima_Chat_project.Models;
using Villa_API_Project.DataAccess.Data;
using Villa_API_Project.DataAccess.Reposatory;

namespace Restaurant.Infrastructure.Reposatory
{
    public class GroupMessagesRepo : Reposatory<GroupMessage>, IGroupMessagesReposatory
    {
        Context Context;
        public GroupMessagesRepo(Context context) : base(context)
        {
            Context = context;

        }

        public void DeleteImageMethod(string file, IWebHostEnvironment env)
        {
            if (!string.IsNullOrEmpty(file))
            {
                var relativePath = file.TrimStart('\\', '/');
                var oldImagePath = Path.Combine(env.WebRootPath, relativePath);

                if (File.Exists(oldImagePath))
                {
                    File.Delete(oldImagePath);
                }
            }
        }

        public string GetImageURL(IFormFile file, IWebHostEnvironment env, MessageType Type)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }
            string folderpath = "";
            if (Type == MessageType.Image)
            {
                folderpath = Path.Combine(env.WebRootPath, "GroupMessages/Images");
            }
            else if (Type == MessageType.Video)
            {
                folderpath = Path.Combine(env.WebRootPath, "GroupMessages/Vedeo");
            }
            else if (Type == MessageType.Voice)
            {
                folderpath = Path.Combine(env.WebRootPath, "GroupMessages/Voice");
            }
            if (!Directory.Exists(folderpath))
            {
                Directory.CreateDirectory(folderpath);
            }
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string path = Path.Combine(folderpath, fileName);
            using (var stream = new FileStream(path, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            if (Type == MessageType.Image)
            {
                return "GroupMessages/Images" + "/" + fileName;
            }
            else if (Type == MessageType.Video)
            {
                return "GroupMessages/Vedeo" + "/" + fileName;
            }
            else
            {
                return "GroupMessages/Voice" + "/" + fileName;
            }


        }
    }
}
