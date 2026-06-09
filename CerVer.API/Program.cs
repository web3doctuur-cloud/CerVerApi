using CerVer.API.Data;
using CerVer.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Allow environment variables to override configuration early
builder.Configuration.AddEnvironmentVariables();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<CertificateService>();

// Bind EmailSettings to IOptions and register EmailService
builder.Services.Configure<CerVer.API.Services.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<CerVer.API.Services.EmailService>();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Validate critical configuration at startup
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = jwtSettingsSection["Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    // Fail fast in non-development to avoid running with insecure defaults
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("JWT secret is missing or too short. Set JwtSettings:Secret via environment variables or user-secrets.");
    else
        Console.WriteLine("Warning: JWT secret is missing or too short. Development only.");
}

var key = Encoding.ASCII.GetBytes(jwtSecret ?? string.Empty);

// Authentication (JWT)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Require HTTPS metadata in production
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettingsSection["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettingsSection["Audience"],
        ClockSkew = TimeSpan.Zero
    };
});

// read allowed origins from configuration 
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins == null || allowedOrigins.Length == 0)
{
    // Default for development only
    allowedOrigins = new[] { "http://localhost:5173", "https://localhost:5173" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Production hardening
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Development features
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Database migrations + seeding (migrate only, do not use EnsureCreated in production)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Program");

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        logger?.LogInformation("Database migrated.");

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Ensure roles
        string[] roles = { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger?.LogInformation("Created role {role}.", role);
            }
        }

        // Seed admin user only if password provided via config/env
        var adminEmail = builder.Configuration["AdminSettings:Email"] ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var adminPassword = builder.Configuration["AdminSettings:Password"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    logger?.LogInformation("Admin user created: {email}", adminEmail);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger?.LogWarning("Failed to create admin user: {errors}", errors);
                }
            }
            else
            {
                logger?.LogInformation("Admin user already exists: {email}", adminEmail);
            }
        }
        else
        {
            logger?.LogWarning("Admin email or password not set. Skipping admin seeding. Set AdminSettings:Email and AdminSettings:Password via env variables for seeding.");
        }
    }
    catch (Exception ex)
    {
        // Reuse the logger declared above instead of declaring a new local with the same name
        logger?.LogError(ex, "An error occurred while migrating or seeding the database.");
        if (app.Environment.IsDevelopment())
        {
            Console.WriteLine("Migration/Seeding error: " + ex);
        }
    }
}

app.Run();