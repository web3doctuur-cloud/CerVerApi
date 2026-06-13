using CerVer.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CerVer.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CertificatesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CertificatesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/certificates/my
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyCertificates()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("userId");

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var certificates = await _context.Certificates
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IssueDate)
                .Select(c => new
                {
                    c.Id,
                    c.MembershipRequestId,
                    c.CertificateNumber,
                    c.SerialNumber,
                    c.FullName,
                    c.MembershipTitle,
                    c.IssueDate,
                    c.ExpiryDate,
                    c.QrCodeUrl,
                    c.PdfPath,
                    c.VerificationUrl,
                    c.IsValid,
                    isExpired = c.ExpiryDate <= DateTime.Today
                })
                .ToListAsync();

            return Ok(certificates);
        }

        // GET: api/certificates/verify/{certificateNumber}
        [AllowAnonymous]
        [HttpGet("verify/{certificateNumber}")]
        public async Task<IActionResult> VerifyCertificate(string certificateNumber)
        {
            if (string.IsNullOrWhiteSpace(certificateNumber))
            {
                return BadRequest(new
                {
                    valid = false,
                    message = "Certificate number is required"
                });
            }

            var certificate = await _context.Certificates
                .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber);

            if (certificate == null)
            {
                return NotFound(new
                {
                    valid = false,
                    message = "Certificate not found"
                });
            }

            var isExpired = certificate.ExpiryDate <= DateTime.Today;
            var valid = certificate.IsValid && !isExpired;

            return Ok(new
            {
                valid,
                message = valid
                    ? "This certificate is authentic and active"
                    : isExpired
                        ? "This certificate is authentic but expired"
                        : "This certificate is no longer valid",
                certificate.Id,
                certificate.CertificateNumber,
                certificate.SerialNumber,
                certificate.FullName,
                certificate.MembershipTitle,
                certificate.IssueDate,
                certificate.ExpiryDate,
                certificate.QrCodeUrl,
                certificate.PdfPath,
                certificate.VerificationUrl,
                certificate.IsValid,
                isExpired
            });
        }

        // GET: api/certificates/download/{certificateNumber}
        [AllowAnonymous]
        [HttpGet("download/{certificateNumber}")]
        public async Task<IActionResult> DownloadCertificate(string certificateNumber)
        {
            var certificate = await _context.Certificates
                .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber);

            if (certificate == null)
            {
                return NotFound(new { message = "Certificate not found" });
            }

            if (string.IsNullOrWhiteSpace(certificate.PdfPath))
            {
                return NotFound(new { message = "Certificate PDF has not been generated" });
            }

            var rootPath = _environment.WebRootPath ?? _environment.ContentRootPath;
            var relativePath = certificate.PdfPath.TrimStart('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var fullPath = Path.Combine(rootPath, relativePath);

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound(new { message = "Certificate PDF file was not found on the server" });
            }

            var fileName = $"{certificate.CertificateNumber}.pdf";
            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

            return File(fileBytes, "application/pdf", fileName);
        }
    }
}