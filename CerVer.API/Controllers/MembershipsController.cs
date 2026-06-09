using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CerVer.API.Data;
using CerVer.API.Models;

namespace CerVer.API.Controllers
{
     
    [Route("api/[controller]")]
    [ApiController] 
    public class MembershipsController : ControllerBase
    {
        // Private field to hold database context
        private readonly ApplicationDbContext _context;
        public MembershipsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/memberships
        // PUBLIC - anyone can view memberships
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Membership>>> GetMemberships()
        {
            var memberships = await _context.Memberships
                .Where(m => m.IsActive)
                .ToListAsync();

            return Ok(memberships);
        }

       
        // GET: api/memberships/all
        // ADMIN ONLY - view all memberships (including inactive)
        [Authorize(Roles = "Admin")] 
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<Membership>>> GetAllMemberships()
        { 
            var memberships = await _context.Memberships.ToListAsync();
            return Ok(memberships);
        }

        // GET: api/memberships/{id}
        // PUBLIC - get a single membership by ID
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Membership>> GetMembership(int id)
        {
            
            var membership = await _context.Memberships.FindAsync(id);

            if (membership == null)
            {
                return NotFound(new { message = $"Membership with ID {id} not found" });
            }
            return Ok(membership);
        }

        
        // POST: api/memberships
        // ADMIN ONLY - create a new membership
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Membership>> CreateMembership([FromBody] Membership membership)
        {
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Set created date
            membership.CreatedAt = DateTime.Now;
            membership.IsActive = true; 

            // Add to database
            _context.Memberships.Add(membership);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMembership), new { id = membership.Id }, membership);
        }

        
        // PUT: api/memberships/{id}
        // ADMIN ONLY - update an existing membership
        
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMembership(int id, [FromBody] Membership membership)
        {
            if (id != membership.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            // Check if membership exists
            var existingMembership = await _context.Memberships.FindAsync(id);
            if (existingMembership == null)
            {
                return NotFound(new { message = $"Membership with ID {id} not found" });
            }

            // Update the fields
            existingMembership.Title = membership.Title;
            existingMembership.Benefits = membership.Benefits;
            existingMembership.Requirements = membership.Requirements;
            existingMembership.ImageUrl = membership.ImageUrl;
            existingMembership.IsActive = membership.IsActive;
            existingMembership.UpdatedAt = DateTime.Now;

            // Mark as modified and save
            _context.Entry(existingMembership).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MembershipExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent(); 
        }

        // DELETE: api/memberships/{id}
        // ADMIN ONLY - delete a membership
       
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMembership(int id)
        {
            var membership = await _context.Memberships.FindAsync(id);
            if (membership == null)
            {
                return NotFound(new { message = $"Membership with ID {id} not found" });
            }

            _context.Memberships.Remove(membership);
            await _context.SaveChangesAsync();

            return NoContent(); 
        }

        private bool MembershipExists(int id)
        {
            return _context.Memberships.Any(e => e.Id == id);
        }
    }
}