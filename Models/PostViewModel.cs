using blogs.Data;
using Microsoft.AspNetCore.Identity;

namespace blogs.Models
{
    public class PostViewModel
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string? AuthorId { get; set; } 

        public string? Content { get; set; }

        public IFormFile? Image { get; set; }

        public List<ApplicationUser>? Authors { get; set; }
        public bool IsAdmin { get; set; } = false;

        public string? CurrentUsername { get; set; }

    }
}
