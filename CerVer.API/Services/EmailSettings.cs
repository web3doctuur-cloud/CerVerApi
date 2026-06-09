using System;

namespace CerVer.API.Services
{
    public class EmailSettings
    {
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? FromName { get; set; } = "CerVer System";
        public string? FromEmail { get; set; } = "noreply@cerver.com";
    }
}