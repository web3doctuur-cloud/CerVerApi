using System.ComponentModel.DataAnnotations;

namespace CerVer.API.Models
{
    public class Certificate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MembershipRequestId { get; set; }

        [Required]
        public string CertificateNumber { get; set; } = string.Empty;

        [Required]
        public string SerialNumber { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        
        [Required]
        public string MembershipTitle { get; set; } = string.Empty;

        [Required]
        public DateTime IssueDate { get; set; } = DateTime.Now;

        public DateTime ExpiryDate { get; set; }

        public string? QrCodeUrl { get; set; }

        public string? PdfPath { get; set; }

        public string VerificationUrl { get; set; } = string.Empty;

        public bool IsValid { get; set; } = true;
    }
}