using Microsoft.AspNetCore.Http;

namespace Realtima_Chat_project.DTO
{
    public class CreateGroupDTO
    {

        public string Name { get; set; }
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }
    }
}
