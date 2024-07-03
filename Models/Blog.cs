using blogs.Data;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace blogs.Models
{
    public class Blog
    {
        public Guid Id { get; set; }

        [MaxLength(256)]
        public required string Title { get; set; }

        public string? Content { get; set; }

        [MaxLength(450)]
        public required string AuthorId { get; set; }

        [DisplayName("Image")]
        public String? ImgSrc { get; set; }

        public DateTime CreatedDateTime { get; set; } = DateTime.Now;

        [ForeignKey("AuthorId")]
        public virtual ApplicationUser? Author { get; set; }
    }
}
