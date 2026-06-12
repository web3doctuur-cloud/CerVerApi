using CerVer.API.Data;
using CerVer.API.Models;
using CerVer.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace CerVer.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MembershipRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly CertificateService _certificateService;
        private readonly FileUploadService _fileUploadService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        // Constructor with all dependencies injected
        public MembershipRequestsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            CertificateService certificateService,
            FileUploadService fileUploadService,
            EmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _certificateService = certificateService;
            _fileUploadService = fileUploadService;
            _emailService = emailService;
            _configuration = configuration;
        }

        
        // GET: api/membershiprequests
        // ADMIN ONLY - Get all membership request
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MembershipRequest>>> GetAllRequests()
        {
            var requests = await _context.MembershipRequests
                .Include(r => r.Membership)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return Ok(requests);
        }

       
        // GET: api/membershiprequests/my
        // USER ONLY - Get current user's own requests
        [Authorize]
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<MembershipRequest>>> GetMyRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var requests = await _context.MembershipRequests
                .Include(r => r.Membership)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return Ok(requests);
        }

        
        // GET: api/membershiprequests/pending
        // ADMIN ONLY - Get pending requests (awaiting approval)

        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<MembershipRequest>>> GetPendingRequests()
        {
            var requests = await _context.MembershipRequests
                .Include(r => r.Membership)
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return Ok(requests);
        }

        
        // POST: api/membershiprequests
        // USER ONLY - Submit a new membership request
        
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<MembershipRequest>> CreateRequest([FromBody] CreateRequestModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Check if membership exists
            var membership = await _context.Memberships.FindAsync(model.MembershipId);
            if (membership == null)
            {
                return BadRequest(new { message = "Membership not found" });
            }

            // Check if user already has a pending request for this membership
            var existingRequest = await _context.MembershipRequests
                .FirstOrDefaultAsync(r => r.UserId == userId &&
                                         r.MembershipId == model.MembershipId &&
                                         r.Status == "Pending");

            if (existingRequest != null)
            {
                return BadRequest(new { message = "You already have a pending request for this membership" });
            }

            // Create new request
            var request = new MembershipRequest
            {
                MembershipId = model.MembershipId,
                UserId = userId,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                RequirementsFile = model.RequirementsFile,
                Status = "Pending",
                RequestedAt = DateTime.Now
            };

            _context.MembershipRequests.Add(request);
            await _context.SaveChangesAsync();

            
            await _emailService.NotifyAdminNewRequest(request.FullName, membership.Title, request.Id);

            return Ok(new
            {
                message = "Request submitted successfully!",
                requestId = request.Id
            });
        }

        
        // PUT: api/membershiprequests/{id}/approve
        // ADMIN ONLY - Approve a membership request
        
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var request = await _context.MembershipRequests
                .Include(r => r.Membership)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound(new { message = "Request not found" });
            }

            if (request.Status == "Approved")
            {
                return BadRequest(new { message = "Request already approved" });
            }

            if (request.Status == "Rejected")
            {
                return BadRequest(new { message = "Cannot approve a rejected request" });
            }

            // Update status
            request.Status = "Approved";
            request.ApprovedAt = DateTime.Now;
            request.CertificateNumber = GenerateCertificateNumber();

            await _context.SaveChangesAsync();

            // Send email notification to user
            await _emailService.NotifyUserRequestApproved(
                request.Email,
                request.FullName,
                request.Membership.Title
            );

            return Ok(new
            {
                message = "Request approved successfully!",
                certificateNumber = request.CertificateNumber
            });
        }

        private string GenerateCertificateNumber()
        {
            // Format: CERT-YYYYMMDD-XXXX
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random();
            var sequence = random.Next(1000, 9999).ToString();
            return $"CERT-{date}-{sequence}";
        }


        // PUT: api/membershiprequests/{id}/reject
        // ADMIN ONLY - Reject a membership request

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectRequest(int id, [FromBody] RejectRequestModel model)
        {
            var request = await _context.MembershipRequests
                .Include(r => r.Membership)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound(new { message = "Request not found" });
            }

            if (request.Status == "Approved")
            {
                return BadRequest(new { message = "Cannot reject an approved request" });
            }

            request.Status = "Rejected";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Request rejected",
                reason = model.Reason
            });
        }


        // POST: api/membershiprequests/{id}/generate-certificate
        // ADMIN ONLY - Generate certificate after approval

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/generate-certificate")]
        public async Task<IActionResult> GenerateCertificate(int id)
        {
            var request = await _context.MembershipRequests
                .Include(r => r.Membership)
                .FirstOrDefaultAsync(r => r.Id == id && r.Status == "Approved");

            if (request == null)
            {
                return NotFound(new { message = "Approved request not found" });
            }

            var existingCertificate = await _context.Certificates
                .FirstOrDefaultAsync(c => c.MembershipRequestId == id);

            if (existingCertificate != null)
            {
                return BadRequest(new { message = "Certificate already generated" });
            }

            try
            {
                var certificateNumber = _certificateService.GenerateCertificateNumber();
                var serialNumber = _certificateService.GenerateSerialNumber();
                var issueDate = DateTime.Now;
                var expiryDate = issueDate.AddYears(2);
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";
                var verificationUrl = $"{baseUrl}/verify/{certificateNumber}";
                var qrCodeBase64 = _certificateService.GenerateQRCode(verificationUrl);

                var html = _certificateService.GenerateCertificateHtml(
                    request.FullName,
                    request.Membership.Title,
                    certificateNumber,
                    serialNumber,
                    issueDate,
                    expiryDate,
                    qrCodeBase64
                );

                var pdfBytes = await _certificateService.GeneratePdfFromHtml(html);
                var pdfPath = await _certificateService.SaveCertificatePdf(pdfBytes, certificateNumber);

                var certificate = new Certificate
                {
                    MembershipRequestId = request.Id,
                    CertificateNumber = certificateNumber,
                    SerialNumber = serialNumber,
                    UserId = request.UserId,
                    FullName = request.FullName,
                    MembershipTitle = request.Membership.Title,
                    IssueDate = issueDate,
                    ExpiryDate = expiryDate,
                    QrCodeUrl = verificationUrl,
                    PdfPath = pdfPath,
                    VerificationUrl = verificationUrl,
                    IsValid = true
                };

                _context.Certificates.Add(certificate);
                request.CertificateNumber = certificateNumber;
                request.CertificatePath = pdfPath;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Certificate generated successfully!",
                    certificateNumber = certificateNumber,
                    downloadUrl = $"/api/certificates/download/{certificateNumber}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // POST: api/membershiprequests/upload-requirements/{requestId}
        // USER ONLY - Upload requirements file for a request

        [Authorize]
        [HttpPost("upload-requirements/{requestId}")]
        public async Task<IActionResult> UploadRequirements(int requestId, IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var request = await _context.MembershipRequests.FindAsync(requestId);

            if (request == null)
            {
                return NotFound(new { message = "Request not found" });
            }

            if (request.UserId != userId)
            {
                return Forbid();
            }

            if (request.Status != "Pending")
            {
                return BadRequest(new { message = "Cannot upload files for processed requests" });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            if (!_fileUploadService.IsValidFile(file))
            {
                return BadRequest(new { message = "Invalid file type. Allowed: PDF, DOC, DOCX, JPG, PNG (Max 5MB)" });
            }

            // Delete old file if exists
            if (!string.IsNullOrEmpty(request.RequirementsFile))
            {
                _fileUploadService.DeleteFile(request.RequirementsFile);
            }

            // Upload new file
            var filePath = await _fileUploadService.UploadFile(file, "Requirements");
            request.RequirementsFile = filePath;

            await _context.SaveChangesAsync();

            return Ok(new { message = "File uploaded successfully", filePath = filePath });
        }

        
        // DELETE: api/membershiprequests/{id}
        // ADMIN ONLY - Delete a request
     
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.MembershipRequests.FindAsync(id);

            if (request == null)
            {
                return NotFound(new { message = "Request not found" });
            }

            // Delete associated file if exists
            if (!string.IsNullOrEmpty(request.RequirementsFile))
            {
                _fileUploadService.DeleteFile(request.RequirementsFile);
            }

            _context.MembershipRequests.Remove(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Request deleted successfully" });
        }

    }

}