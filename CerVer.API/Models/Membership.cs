using System.ComponentModel.DataAnnotations;
namespace CerVer.API.Models
{
    
    public class Membership
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Membership title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; } = string.Empty; 

        [Required(ErrorMessage = "Benefits are required")]
        public string Benefits { get; set; } = string.Empty;

        [Required(ErrorMessage = "Requirements are required")]
        public string Requirements { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true; 

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}