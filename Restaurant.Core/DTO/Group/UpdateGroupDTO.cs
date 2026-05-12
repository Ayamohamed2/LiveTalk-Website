using Microsoft.AspNetCore.Http;

namespace Realtima_Chat_project.DTO
{
    public class UpdateGroupDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }

    }
}
