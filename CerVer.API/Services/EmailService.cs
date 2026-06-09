using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace CerVer.API.Services
{
    // Main Email Service - handles all email notifications
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly EmailSettings _settings;

        // EmailSettings now bound via IOptions
        public EmailService(IConfiguration configuration, IHostEnvironment environment, IOptions<EmailSettings> options)
        {
            _configuration = configuration;
            _environment = environment;
            _settings = options?.Value ?? LoadEmailSettings();
            ApplyEnvironmentOverrides();
        }

        // Load email settings from configuration or environment variables (fallback)
        private EmailSettings LoadEmailSettings()
        {
            // For development, read from appsettings.json
            return _configuration.GetSection("EmailSettings").Get<EmailSettings>() ?? new EmailSettings
            {
                Host = "smtp.gmail.com",
                Port = 587,
                UseSsl = true,
                Username = null,
                Password = null,
                FromName = "CerVer System",
                FromEmail = "noreply@cerver.com"
            };
        }

        // Allow environment variables to override configured values (production-safe)
        private void ApplyEnvironmentOverrides()
        {
            var host = Environment.GetEnvironmentVariable("EMAIL_HOST") ?? Environment.GetEnvironmentVariable("EmailSettings__Host");
            if (!string.IsNullOrWhiteSpace(host)) _settings.Host = host;

            var portStr = Environment.GetEnvironmentVariable("EMAIL_PORT") ?? Environment.GetEnvironmentVariable("EmailSettings__Port");
            if (int.TryParse(portStr, out var port)) _settings.Port = port;

            var useSslStr = Environment.GetEnvironmentVariable("EMAIL_USESSL") ?? Environment.GetEnvironmentVariable("EmailSettings__UseSsl");
            if (bool.TryParse(useSslStr, out var useSsl)) _settings.UseSsl = useSsl;

            var user = Environment.GetEnvironmentVariable("EMAIL_USERNAME") ?? Environment.GetEnvironmentVariable("EmailSettings__Username");
            if (!string.IsNullOrWhiteSpace(user)) _settings.Username = user;

            var pass = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? Environment.GetEnvironmentVariable("EmailSettings__Password");
            if (!string.IsNullOrWhiteSpace(pass)) _settings.Password = pass;

            var fromName = Environment.GetEnvironmentVariable("EMAIL_FROMNAME") ?? Environment.GetEnvironmentVariable("EmailSettings__FromName");
            if (!string.IsNullOrWhiteSpace(fromName)) _settings.FromName = fromName;

            var from = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? Environment.GetEnvironmentVariable("EmailSettings__FromEmail");
            if (!string.IsNullOrWhiteSpace(from)) _settings.FromEmail = from;

            // sensible defaults
            _settings.Host ??= "smtp.example.com";
            if (_settings.Port == 0) _settings.Port = 587;
            _settings.FromName ??= "CerVer System";
            _settings.FromEmail ??= "noreply@cerver.com";
        }

        // Core email sending method
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                Console.WriteLine("[EmailService] Error: No recipient email provided");
                return false;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName!, _settings.FromEmail!));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                Console.WriteLine($"[EmailService] Attempting to send email to: {toEmail}");
                Console.WriteLine($"[EmailService] Using SMTP: {_settings.Host}:{_settings.Port}");

                // If credentials missing, log the email and skip actual send (development convenience)
                if (string.IsNullOrWhiteSpace(_settings.Username) || string.IsNullOrWhiteSpace(_settings.Password))
                {
                    Console.WriteLine($"[EmailService] WARNING: Email credentials not configured. Email would have been sent to: {toEmail}");
                    Console.WriteLine($"[EmailService] EMAIL CONTENT:\n{body}");
                    return true;
                }

                using var client = new SmtpClient();

                if (_settings.Port == 465)
                {
                    await client.ConnectAsync(_settings.Host!, _settings.Port, SecureSocketOptions.SslOnConnect);
                }
                else
                {
                    // Prefer StartTls for 587 and other sub-465 ports
                    await client.ConnectAsync(_settings.Host!, _settings.Port, SecureSocketOptions.StartTls);
                }

                await client.AuthenticateAsync(_settings.Username!, _settings.Password!);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"[EmailService] Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] ERROR sending email to {toEmail}: {ex.Message}");
                Console.WriteLine($"[EmailService] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        // 1. Notify admin when a new membership request is submitted
        public async Task NotifyAdminNewRequest(string userName, string membershipTitle, int requestId)
        {
            var subject = "🔔 New Membership Request Received - CerVer";

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .button {{ background: #667eea; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #999; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>New Membership Request</h2>
                        </div>
                        <div class='content'>
                            <p>A new membership request has been submitted and requires your review.</p>
                            <h3>Request Details:</h3>
                            <ul>
                                <li><strong>User:</strong> {userName}</li>
                                <li><strong>Membership Type:</strong> {membershipTitle}</li>
                                <li><strong>Request ID:</strong> {requestId}</li>
                            </ul>
                            <p>Please login to the admin dashboard to review this request.</p>
                            <p>
                                <a href='{baseUrl}/admin/requests/{requestId}' class='button'>Review Request</a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>CerVer Certificate System • Automated Notification</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            var adminEmail = _configuration["AppSettings:AdminEmail"] ?? "admin@cerver.com";
            await SendEmailAsync(adminEmail, subject, body);
        }

        // 2. Notify user when their request is approved
        public async Task NotifyUserRequestApproved(string userEmail, string userName, string membershipTitle)
        {
            var subject = "🎉 Congratulations! Your Membership Request Has Been Approved";

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .button {{ background: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #999; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Congratulations {userName}!</h2>
                        </div>
                        <div class='content'>
                            <p>Great news! Your request for the <strong>{membershipTitle}</strong> has been approved.</p>
                            <p>Your certificate will be generated shortly. Once ready, you'll be able to download it from your dashboard.</p>
                            <p>
                                <a href='{baseUrl}/dashboard' class='button'>Go to Dashboard</a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>CerVer Certificate System • Automated Notification</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(userEmail, subject, body);
        }

        // 3. Notify user when certificate is ready for download
        public async Task NotifyCertificateReady(string userEmail, string userName, string certificateNumber, string membershipTitle)
        {
            var subject = "📜 Your Certificate is Ready for Download";

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #d4af37 0%, #ffd700 100%); color: #2c3e50; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .certificate-box {{ background: #f8f9fa; border: 2px solid #d4af37; border-radius: 10px; padding: 15px; margin: 20px 0; text-align: center; }}
                        .certificate-number {{ font-size: 18px; font-weight: bold; color: #d4af37; }}
                        .button {{ background: #28a745; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold; }}
                        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #999; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Your Certificate is Ready! 🎓</h2>
                        </div>
                        <div class='content'>
                            <p>Dear {userName},</p>
                            <p>Your <strong>{membershipTitle}</strong> certificate has been generated successfully.</p>
                            
                            <div class='certificate-box'>
                                <p><strong>Certificate Number:</strong></p>
                                <p class='certificate-number'>{certificateNumber}</p>
                                <p>Scan the QR code on your certificate to verify its authenticity.</p>
                            </div>
                            
                            <p style='text-align: center;'>
                                <a href='{baseUrl}/dashboard' class='button'>📥 Download Your Certificate</a>
                            </p>
                            <p style='text-align: center; font-size: 14px;'>
                                Anyone can verify your certificate using the QR code or by visiting:<br/>
                                <a href='{baseUrl}/verify'>{baseUrl}/verify</a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>CerVer Certificate System • Automated Notification</p>
                            <p><small>This certificate expires 2 years from issue date.</small></p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(userEmail, subject, body);
        }

        // 4. Welcome email for new users
        public async Task SendWelcomeEmail(string userEmail, string userName)
        {
            var subject = "Welcome to CerVer Certificate System!";

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .button {{ background: #667eea; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #999; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Welcome to CerVer, {userName}! 🎉</h2>
                        </div>
                        <div class='content'>
                            <p>Thank you for joining CerVer - The Digital Certificate Verification System.</p>
                            <p>With CerVer, you can:</p>
                            <ul>
                                <li>Apply for professional memberships</li>
                                <li>Receive verifiable digital certificates</li>
                                <li>Share your achievements with QR codes</li>
                                <li>Verify anyone's certificate instantly</li>
                            </ul>
                            <p>
                                <a href='{baseUrl}/memberships' class='button'>Explore Memberships</a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>CerVer Certificate System • Automated Notification</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(userEmail, subject, body);
        }

        // 5. Password reset email (for future implementation)
        public async Task SendPasswordResetEmail(string userEmail, string userName, string resetToken)
        {
            var subject = "Password Reset Request - CerVer";

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";
            var resetLink = $"{baseUrl}/reset-password?token={resetToken}&email={userEmail}";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #dc3545; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .button {{ background: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                        .warning {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #999; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Password Reset Request</h2>
                        </div>
                        <div class='content'>
                            <p>Hello {userName},</p>
                            <p>We received a request to reset your password. Click the button below to create a new password:</p>
                            <p style='text-align: center;'>
                                <a href='{resetLink}' class='button'>Reset Password</a>
                            </p>
                            <div class='warning'>
                                <p><strong>⚠️ This link will expire in 1 hour.</strong></p>
                                <p>If you didn't request this, please ignore this email. Your password will remain unchanged.</p>
                            </div>
                            <p>Or copy and paste this link into your browser:</p>
                            <p><small>{resetLink}</small></p>
                        </div>
                        <div class='footer'>
                            <p>CerVer Certificate System • Automated Notification</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(userEmail, subject, body);
        }

        // 6. Certificate expiry reminder (for future implementation)
        public async Task SendCertificateExpiryReminder(string userEmail, string userName, string certificateNumber, DateTime expiryDate)
        {
            var subject = "⏰ Certificate Expiry Reminder - CerVer";

            var daysUntilExpiry = (expiryDate - DateTime.Now).Days;
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #ffc107; color: #333; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .button {{ background: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #999; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Certificate Expiry Reminder</h2>
                        </div>
                        <div class='content'>
                            <p>Dear {userName},</p>
                            <p>Your certificate <strong>{certificateNumber}</strong> will expire in <strong>{daysUntilExpiry} days</strong>.</p>
                            <p>To maintain your active status, please renew your membership before:</p>
                            <p><strong>{expiryDate:MMMM dd, yyyy}</strong></p>
                            <p style='text-align: center;'>
                                <a href='{baseUrl}/memberships' class='button'>Renew Membership</a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>CerVer Certificate System • Automated Notification</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(userEmail, subject, body);
        }
    }
}