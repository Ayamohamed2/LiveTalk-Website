using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Restaurant.Core.Models.Account;
using Villa_API_Project.DataAccess.Data;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;
using static System.Net.Mime.MediaTypeNames;

namespace Villa_API_Project.DataAccess.Reposatory
{
    public class ApplicationUserReposatory : Reposatory<ApplicationUser>, IAPPlicationUserReposatory
    {
       
        Context Context;
        public ApplicationUserReposatory(Context context) : base(context)
        {
            this.Context = context;
         
        }
        
        public void DeleteImageMethod(string imageURL, IWebHostEnvironment env)
        {
            if ( !string.IsNullOrEmpty(imageURL))
            {
                var relativePath = imageURL.TrimStart('\\', '/');
                var oldImagePath = Path.Combine(env.WebRootPath, relativePath);

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

        }

        public string GetImageURL(IFormFile ImageFile, string id, IWebHostEnvironment env)
        {
            if (ImageFile == null || ImageFile.Length == 0)
            {
                return null;
            }

            string folderpath = Path.Combine(env.WebRootPath, "Images/Profile" + id);
            if (!Directory.Exists(folderpath))
            {
                Directory.CreateDirectory(folderpath);
            }
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
            string path = Path.Combine(folderpath, fileName);
            using (var stream = new FileStream(path, FileMode.Create))
            {
                ImageFile.CopyTo(stream);
            }
            

            return "/Images/Profile" + id + "/" + fileName;


        }
    }
}
    
