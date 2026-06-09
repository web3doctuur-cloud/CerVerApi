using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CerVer.API.Models
{
    public class MembershipRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MembershipId { get; set; }

        [ForeignKey("MembershipId")]
        public Membership? Membership { get; set; } 

       
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? RequirementsFile { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime RequestedAt { get; set; } = DateTime.Now;
      
        public DateTime? ApprovedAt { get; set; }

        public string? CertificateNumber { get; set; }

        public string? CertificatePath { get; set; }
    }
}