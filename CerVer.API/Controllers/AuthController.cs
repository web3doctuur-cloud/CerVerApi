using CerVer.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CerVer.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // These services handle user management and signing
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        // Constructor - gets services via Dependency Injection
        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _environment = environment;
        }

        // REGISTER - Create a new user account
        // POST: api/auth/register
        
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            // Check if model is valid (all required fields filled)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Create new user object
            var user = new IdentityUser
            {
                UserName = model.Email,  
                Email = model.Email,
                EmailConfirmed = true    // Skip email verification for now
            };

            // Try to create the user in database
            var result = await _userManager.CreateAsync(user, model.Password);

           
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            // Add user to "User" role (not admin)
            await _userManager.AddToRoleAsync(user, "User");

            // Return success message
            return Ok(new { message = "User registered successfully!" });
        }

        // LOGIN - Authenticate user and return JWT token
        // POST: api/auth/login
        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // Check if model is valid
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Check password
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            // If password is wrong
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Generate JWT token for this user
            var token = await GenerateJwtToken(user);

            // Get user's roles
            var roles = await _userManager.GetRolesAsync(user);

            // Return token and user info
            return Ok(new
            {
                token = token,
                email = user.Email,
                roles = roles,
                userId = user.Id
            });
        }

        // HELPER: Generate JWT Token
        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            // Get JWT settings from appsettings.json
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"];
            var key = Encoding.ASCII.GetBytes(secretKey);
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"]);

            // Get user's roles
            var roles = await _userManager.GetRolesAsync(user);

            // Create claims (pieces of information stored in the token)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),     
                new Claim(JwtRegisteredClaimNames.Email, user.Email), 
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), 
                new Claim("userId", user.Id)                          
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Create signing credentials
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256);

            // Create the token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            // Return the token as a string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // DEVELOPMENT ONLY: Promote user to admin (for fixing existing users)
        [HttpPost("promote-to-admin")]
        public async Task<IActionResult> PromoteToAdmin([FromQuery] string email)
        {
            if (!_environment.IsDevelopment())
            {
                return Forbid();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            return Ok(new { message = $"User {email} promoted to Admin" });
        }
    }

}
