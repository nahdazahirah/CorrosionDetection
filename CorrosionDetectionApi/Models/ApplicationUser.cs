using Microsoft.AspNetCore.Identity;

namespace CorrosionDetection.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = "";
    }
}