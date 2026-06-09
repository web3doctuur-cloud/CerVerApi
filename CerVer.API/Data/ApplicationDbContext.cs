using CerVer.API.Models; // This imports our models
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CerVer.API.Data
{
    
    public class ApplicationDbContext : IdentityDbContext<Microsoft.AspNetCore.Identity.IdentityUser> 
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) 
        {
        }

        public DbSet<Membership> Memberships { get; set; }

        public DbSet<MembershipRequest> MembershipRequests { get; set; }

        public DbSet<Certificate> Certificates { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // We'll add seed data (initial admin user) here later
            // For now, it's empty but the parent class handles user tables
        }
    }
}