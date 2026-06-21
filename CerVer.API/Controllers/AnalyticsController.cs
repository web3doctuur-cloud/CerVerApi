using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CerVer.API.Data;
using CerVer.API.Models;

namespace CerVer.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Only Admin can access analytics
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AnalyticsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/analytics/dashboard
        // Get main dashboard statistics
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            // Get current date ranges
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            // Count statistics
            var totalMemberships = await _context.Memberships.CountAsync();
            var totalRequests = await _context.MembershipRequests.CountAsync();
            var pendingRequests = await _context.MembershipRequests.CountAsync(r => r.Status == "Pending");
            var approvedRequests = await _context.MembershipRequests.CountAsync(r => r.Status == "Approved");
            var rejectedRequests = await _context.MembershipRequests.CountAsync(r => r.Status == "Rejected");

            // Certificate statistics
            var totalCertificates = await _context.Certificates.CountAsync();
            var activeCertificates = await _context.Certificates
                .CountAsync(c => c.IsValid && c.ExpiryDate > today);
            var expiredCertificates = await _context.Certificates
                .CountAsync(c => c.ExpiryDate <= today);

            // User statistics
            var totalUsers = await _userManager.Users.CountAsync();

            // If your Identity user class records creation date (e.g., ApplicationUser.CreatedOn),
            // replace this fallback with the actual property. For now, set to 0 to avoid compile errors.
            var newUsersThisMonth = 0;

            // Monthly requests (current year)
            var monthlyRequests = await _context.MembershipRequests
                .Where(r => r.RequestedAt.Year == today.Year)
                .GroupBy(r => r.RequestedAt.Month)
                .Select(g => new {
                    Month = g.Key,
                    MonthName = GetMonthName(g.Key),
                    Total = g.Count(),
                    Approved = g.Count(r => r.Status == "Approved"),
                    Rejected = g.Count(r => r.Status == "Rejected"),
                    Pending = g.Count(r => r.Status == "Pending")
                })
                .OrderBy(g => g.Month)
                .ToListAsync();

            return Ok(new
            {
                memberships = new
                {
                    total = totalMemberships
                },
                requests = new
                {
                    total = totalRequests,
                    pending = pendingRequests,
                    approved = approvedRequests,
                    rejected = rejectedRequests
                },
                certificates = new
                {
                    total = totalCertificates,
                    active = activeCertificates,
                    expired = expiredCertificates
                },
                users = new
                {
                    total = totalUsers,
                    newThisMonth = newUsersThisMonth
                },
                monthlyStats = monthlyRequests,
                lastUpdated = DateTime.Now
            });
        }

        // GET: api/analytics/membership-popularity
        // Most popular memberships
        [HttpGet("membership-popularity")]
        public async Task<IActionResult> GetMembershipPopularity()
        {
            // First get all approved requests with their membership info, filtering out nulls
            var approvedRequests = await _context.MembershipRequests
                .Include(r => r.Membership)
                .Where(r => r.Status == "Approved" && r.Membership != null)
                .ToListAsync();

            // Now group in-memory to avoid EF Core issues
            var popularMemberships = approvedRequests
                .GroupBy(r => new { r.Membership.Id, r.Membership.Title })
                .Select(g => new
                {
                    membershipId = g.Key.Id,
                    membershipTitle = g.Key.Title,
                    requestCount = g.Count(),
                    percentage = 0.0
                })
                .OrderByDescending(g => g.requestCount)
                .ToList();

            var totalApproved = approvedRequests.Count;

            // Calculate percentages
            foreach (var item in popularMemberships)
            {
                var percentage = totalApproved > 0
                    ? Math.Round((double)item.requestCount / totalApproved * 100, 2)
                    : 0;
                // Instead of reflection, recreate the list with percentages
            }
            
            // Recreate the list with proper percentage values
            var result = popularMemberships.Select(item => new 
            {
                item.membershipId,
                item.membershipTitle,
                item.requestCount,
                percentage = totalApproved > 0 ? Math.Round((double)item.requestCount / totalApproved * 100, 2) : 0
            }).ToList();

            return Ok(result);
        }

        // GET: api/analytics/recent-activity
        // Recent 20 activities
        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity()
        {
            // Get recent requests (filter out null memberships)
            var recentRequestsData = await _context.MembershipRequests
                .Include(r => r.Membership)
                .OrderByDescending(r => r.RequestedAt)
                .Take(20)
                .ToListAsync();
                
            var recentRequests = recentRequestsData
                .Where(r => r.Membership != null)
                .Select(r => new
                {
                    id = r.Id,
                    type = "Request",
                    user = r.FullName,
                    membership = r.Membership.Title,
                    status = r.Status,
                    timestamp = r.RequestedAt,
                    message = $"New {r.Membership.Title} request from {r.FullName}"
                })
                .ToList();

            // Get recent approvals
            var recentApprovalsData = await _context.MembershipRequests
                .Include(r => r.Membership)
                .Where(r => r.ApprovedAt.HasValue && r.Membership != null)
                .OrderByDescending(r => r.ApprovedAt)
                .Take(20)
                .ToListAsync();
                
            var recentApprovals = recentApprovalsData
                .Select(r => new
                {
                    id = r.Id,
                    type = "Approval",
                    user = r.FullName,
                    membership = r.Membership.Title,
                    status = "Approved",
                    timestamp = r.ApprovedAt.Value,
                    message = $"Membership request approved for {r.FullName}"
                })
                .ToList();

            // Get recent certificates
            var recentCertificates = await _context.Certificates
                .OrderByDescending(c => c.IssueDate)
                .Take(20)
                .Select(c => new
                {
                    id = c.Id,
                    type = "Certificate",
                    user = c.FullName,
                    membership = c.MembershipTitle,
                    status = "Generated",
                    timestamp = c.IssueDate,
                    message = $"Certificate generated for {c.FullName} - {c.CertificateNumber}"
                })
                .ToListAsync();

            // Combine and sort all activities
            var allActivities = recentRequests
                .Concat(recentApprovals)
                .Concat(recentCertificates)
                .OrderByDescending(a => a.timestamp)
                .Take(30)
                .ToList();

            return Ok(allActivities);
        }

        // GET: api/analytics/certificate-timeline
        // Certificate generation timeline
        [HttpGet("certificate-timeline")]
        public async Task<IActionResult> GetCertificateTimeline()
        {
            var today = DateTime.Today;
            var last6Months = new List<DateTime>();

            // Get last 6 months
            for (int i = 5; i >= 0; i--)
            {
                last6Months.Add(today.AddMonths(-i));
            }

            var timeline = new List<object>();

            foreach (var month in last6Months)
            {
                var startDate = new DateTime(month.Year, month.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var certificateCount = await _context.Certificates
                    .CountAsync(c => c.IssueDate >= startDate && c.IssueDate <= endDate);

                var requestCount = await _context.MembershipRequests
                    .CountAsync(r => r.RequestedAt >= startDate && r.RequestedAt <= endDate);

                timeline.Add(new
                {
                    month = month.ToString("MMM yyyy"),
                    year = month.Year,
                    monthNumber = month.Month,
                    certificates = certificateCount,
                    requests = requestCount,
                    approvalRate = requestCount > 0
                        ? Math.Round((double)certificateCount / requestCount * 100, 2)
                        : 0
                });
            }

            return Ok(timeline);
        }

        // GET: api/analytics/export-summary
        // Export summary data (for reports)
        [HttpGet("export-summary")]
        public async Task<IActionResult> GetExportSummary()
        {
            var today = DateTime.Today;
            var startOfYear = new DateTime(today.Year, 1, 1);

            var summary = new
            {
                generatedAt = DateTime.Now,
                dateRange = new
                {
                    from = startOfYear,
                    to = today
                },
                totals = new
                {
                    totalMemberships = await _context.Memberships.CountAsync(),
                    totalRequests = await _context.MembershipRequests.CountAsync(),
                    totalApproved = await _context.MembershipRequests.CountAsync(r => r.Status == "Approved"),
                    totalRejected = await _context.MembershipRequests.CountAsync(r => r.Status == "Rejected"),
                    totalCertificates = await _context.Certificates.CountAsync(),
                    activeCertificates = await _context.Certificates.CountAsync(c => c.IsValid && c.ExpiryDate > today),
                    totalUsers = await _userManager.Users.CountAsync()
                },
                certificatesByMembership = await _context.Certificates
                    .GroupBy(c => c.MembershipTitle)
                    .Select(g => new
                    {
                        membership = g.Key,
                        count = g.Count()
                    })
                    .OrderByDescending(g => g.count)
                    .ToListAsync()
            };

            return Ok(summary);
        }

        // GET: api/analytics/performance
        // System performance metrics
        [HttpGet("performance")]
        public async Task<IActionResult> GetPerformanceMetrics()
        {
            // Average processing time (days between request and approval)
            var approvedRequests = await _context.MembershipRequests
                .Where(r => r.Status == "Approved" && r.ApprovedAt.HasValue)
                .Select(r => new { r.RequestedAt, r.ApprovedAt })
                .ToListAsync();

            var avgProcessingDays = approvedRequests.Any()
                ? approvedRequests.Average(r => (r.ApprovedAt.Value - r.RequestedAt).TotalDays)
                : 0;

            // Peak request hours (by hour of day)
            var requestsByHour = await _context.MembershipRequests
                .GroupBy(r => r.RequestedAt.Hour)
                .Select(g => new
                {
                    hour = g.Key,
                    count = g.Count()
                })
                .OrderBy(g => g.hour)
                .ToListAsync();

            // Most active day of week
            var requestsByDayOfWeek = await _context.MembershipRequests
                .GroupBy(r => r.RequestedAt.DayOfWeek.ToString())
                .Select(g => new
                {
                    day = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(g => g.count)
                .ToListAsync();

            return Ok(new
            {
                averageProcessingDays = Math.Round(avgProcessingDays, 2),
                requestsByHour = requestsByHour,
                mostActiveDays = requestsByDayOfWeek,
                conversionRate = await GetConversionRate()
            });
        }

        // HELPER METHODS
        private string GetMonthName(int month)
        {
            return new DateTime(2000, month, 1).ToString("MMMM");
        }
         
        private async Task<double> GetConversionRate()
        {
            var totalRequests = await _context.MembershipRequests.CountAsync();
            var approvedRequests = await _context.MembershipRequests.CountAsync(r => r.Status == "Approved");

            if (totalRequests == 0) return 0;

            return Math.Round((double)approvedRequests / totalRequests * 100, 2);
        }
    }
}