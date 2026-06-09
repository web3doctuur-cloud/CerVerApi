using Microsoft.AspNetCore.Http;

namespace CerVer.API.Services
{
    public class FileUploadService
    {
        private readonly IWebHostEnvironment _environment;

        public FileUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        // Upload file and return file path
        public async Task<string> UploadFile(IFormFile file, string subFolder = "Requirements")
        {
            // Create uploads folder if not exists
            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "Uploads", subFolder);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/Uploads/{subFolder}/{uniqueFileName}";
        }

        // Delete file
        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            var fullPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, filePath.TrimStart('/'));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        // Validate file type
        public bool IsValidFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            return allowedExtensions.Contains(extension) && file.Length > 0 && file.Length < 5 * 1024 * 1024; // 5MB limit
        }
    }
}