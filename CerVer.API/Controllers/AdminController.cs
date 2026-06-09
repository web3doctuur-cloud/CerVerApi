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
    [Authorize(Roles = "Admin")] // ALL endpoints in this controller require Admin role
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

       
        // GET: api/admin/dashboard
        // Get statistics for admin dashboard
       
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalMemberships = await _context.Memberships.CountAsync();
            var totalRequests = await _context.MembershipRequests.CountAsync();
            var pendingRequests = await _context.MembershipRequests.CountAsync(r => r.Status == "Pending");
            var approvedRequests = await _context.MembershipRequests.CountAsync(r => r.Status == "Approved");
            var totalUsers = await _userManager.Users.CountAsync();

            return Ok(new
            {
                totalMemberships,
                totalRequests,
                pendingRequests,
                approvedRequests,
                totalUsers
            });
        }

        
        // GET: api/admin/users
        // Get all registered users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.UserName,
                    u.EmailConfirmed
                })
                .ToListAsync();

            // Get roles for each user
            var usersWithRoles = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(
                    await _userManager.FindByIdAsync(user.Id));

                usersWithRoles.Add(new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    Roles = roles
                });
            }

            return Ok(usersWithRoles);
        }
    }

}
