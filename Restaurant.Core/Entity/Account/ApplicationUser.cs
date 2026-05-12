using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant.Core.Models.Account
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string Name { get; set; }
        [ValidateNever]
        public string? ImageURL { get; set; } = "/Images/default.png";
        [NotMapped]
        public IFormFile? Imagefile { get; set; }
        [ValidateNever]
        public DateTime Lastseen { get; set; }
    }
}
