
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System;

namespace CerVer.API.Services
{
    public class CertificateService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public CertificateService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        
        // Generate Certificate Number (Unique identifier)
        public string GenerateCertificateNumber()
        {
            
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random();
            var sequence = random.Next(100000, 999999).ToString();
            return $"CERT-{date}-{sequence}";
        }

        
        // Generate Serial Number (GUID - Globally Unique Identifier)
        public string GenerateSerialNumber()
        {
            return Guid.NewGuid().ToString().ToUpper();
        }

        // Generate QR Code as Base64 string
        public string GenerateQRCode(string verificationUrl)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                // Create QR code data
                var qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);

                // Generate QR code as bitmap
                using (var qrCode = new QRCode(qrCodeData))
                {
                    using (var qrBitmap = qrCode.GetGraphic(20))
                    {
                        // Convert bitmap to Base64 string
                        using (var stream = new MemoryStream())
                        {
                            qrBitmap.Save(stream, ImageFormat.Png);
                            var bytes = stream.ToArray();
                            return Convert.ToBase64String(bytes);
                        }
                    }
                }
            }
        }

       
        // Generate HTML Certificate Template
        
        public string GenerateCertificateHtml(
            string fullName,
            string membershipTitle,
            string certificateNumber,
            string serialNumber,
            DateTime issueDate,
            DateTime expiryDate,
            string qrCodeBase64)
        {
            // Get company name from settings or use default
            var companyName = "CerVer";
            var presidentName = "Dr. John Smith";
            var presidentTitle = "President";

            // Format dates
            var issueDateFormatted = issueDate.ToString("MMMM dd, yyyy");
            var expiryDateFormatted = expiryDate.ToString("MMMM dd, yyyy");

            // Get base URL for verification
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";

           
            var template = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Certificate of Membership - __FULLNAME__</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: 'Times New Roman', serif;
            background-color: #f0f0f0;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            padding: 20px;
        }
        
        .certificate {
            width: 900px;
            background: white;
            border: 15px solid #d4af37;
            padding: 40px;
            position: relative;
            box-shadow: 0 10px 30px rgba(0,0,0,0.2);
        }
        
        .certificate::before {
            content: '';
            position: absolute;
            top: 15px;
            left: 15px;
            right: 15px;
            bottom: 15px;
            border: 2px solid #d4af37;
            pointer-events: none;
        }
        
        .header {
            text-align: center;
            margin-bottom: 30px;
        }
        
        .company-name {
            font-size: 36px;
            font-weight: bold;
            color: #2c3e50;
            letter-spacing: 3px;
        }
        
        .certificate-title {
            font-size: 48px;
            color: #d4af37;
            margin: 20px 0;
            text-transform: uppercase;
            font-weight: bold;
        }
        
        .award-text {
            font-size: 20px;
            text-align: center;
            margin: 30px 0;
        }
        
        .recipient-name {
            font-size: 42px;
            font-weight: bold;
            text-align: center;
            color: #2c3e50;
            margin: 20px 0;
            font-family: 'Georgia', serif;
        }
        
        .membership-info {
            text-align: center;
            font-size: 18px;
            margin: 20px 0;
        }
        
        .membership-badge {
            display: inline-block;
            background: #d4af37;
            color: #2c3e50;
            padding: 5px 20px;
            border-radius: 25px;
            font-weight: bold;
        }
        
        .details {
            margin: 30px 0;
            display: flex;
            justify-content: space-between;
            flex-wrap: wrap;
        }
        
        .detail-item {
            width: 45%;
            margin: 10px 0;
        }
        
        .detail-label {
            font-weight: bold;
            color: #555;
        }
        
        .qr-section {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
        }
        
        .qr-code {
            text-align: center;
        }
        
        .qr-code img {
            width: 100px;
            height: 100px;
        }
        
        .verification-text {
            flex: 1;
            margin-left: 20px;
            font-size: 12px;
            color: #666;
        }
        
        .signatures {
            margin-top: 40px;
            display: flex;
            justify-content: space-between;
        }
        
        .signature {
            text-align: center;
        }
        
        .signature-line {
            width: 200px;
            border-top: 1px solid #333;
            margin: 10px 0;
        }
        
        .signature-name {
            font-weight: bold;
        }
        
        .footer {
            text-align: center;
            margin-top: 30px;
            font-size: 12px;
            color: #999;
        }
        
        @media print {
            body {
                background: white;
                padding: 0;
            }
            .certificate {
                box-shadow: none;
                margin: 0;
                padding: 20px;
            }
        }
    </style>
</head>
<body>
    <div class='certificate'>
        <div class='header'>
            <div class='company-name'>__COMPANY__</div>
            <div class='certificate-title'>Certificate of Membership</div>
        </div>
        
        <div class='award-text'>
            This certificate is proudly presented to
        </div>
        
        <div class='recipient-name'>__FULLNAME__</div>
        
        <div class='membership-info'>
            in recognition of being awarded the
            <div class='membership-badge'>__MEMBERSHIPTITLE__</div>
        </div>
        
        <div class='details'>
            <div class='detail-item'>
                <div class='detail-label'>Certificate Number:</div>
                <div>__CERTIFICATENUMBER__</div>
            </div>
            <div class='detail-item'>
                <div class='detail-label'>Serial Number:</div>
                <div>__SERIALNUMBER__</div>
            </div>
            <div class='detail-item'>
                <div class='detail-label'>Issue Date:</div>
                <div>__ISSUEDATE__</div>
            </div>
            <div class='detail-item'>
                <div class='detail-label'>Expiry Date:</div>
                <div>__EXPIRYDATE__</div>
            </div>
        </div>
        
        <div class='qr-section'>
            <div class='qr-code'>
                <img src='data:image/png;base64,__QRCODE__' alt='QR Code' />
            </div>
            <div class='verification-text'>
                <strong>Verify this certificate</strong><br/>
                Scan this QR code or visit:<br/>
                __BASEURL__/verify/__CERTIFICATENUMBER__<br/>
                <span style='color: #d4af37;'>Valid until __EXPIRYDATE__</span>
            </div>
        </div>
        
        <div class='signatures'>
            <div class='signature'>
                <div class='signature-line'></div>
                <div class='signature-name'>__PRESIDENTNAME__</div>
                <div>__PRESIDENTTITLE__</div>
            </div>
            <div class='signature'>
                <div class='signature-line'></div>
                <div class='signature-name'>__COMPANY__</div>
                <div>Corporate Seal</div>
            </div>
        </div>
        
        <div class='footer'>
            This certificate is the property of __COMPANY__ and cannot be duplicated.<br/>
            Verify authenticity at __BASEURL__/verify
        </div>
    </div>
</body>
</html>";

            var html = template
                .Replace("__COMPANY__", companyName)
                .Replace("__PRESIDENTNAME__", presidentName)
                .Replace("__PRESIDENTTITLE__", presidentTitle)
                .Replace("__FULLNAME__", fullName)
                .Replace("__MEMBERSHIPTITLE__", membershipTitle)
                .Replace("__CERTIFICATENUMBER__", certificateNumber)
                .Replace("__SERIALNUMBER__", serialNumber)
                .Replace("__ISSUEDATE__", issueDateFormatted)
                .Replace("__EXPIRYDATE__", expiryDateFormatted)
                .Replace("__QRCODE__", qrCodeBase64)
                .Replace("__BASEURL__", baseUrl);

            return html;
        }

        
        // Generate PDF from HTML
        
        public async Task<byte[]> GeneratePdfFromHtml(string html)
        {
            // Using Select.HtmlToPdf
            var converter = new Select.HtmlToPdf.HtmlToPdfConverter();
            var pdfBytes = converter.ConvertToPdf(html);

            return await Task.FromResult(pdfBytes);
        }

       
        // Save Certificate PDF to disk
        
        public async Task<string> SaveCertificatePdf(byte[] pdfBytes, string certificateNumber)
        {
            // Create certificates folder if not exists
            var certificatesFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "Certificates");

            if (!Directory.Exists(certificatesFolder))
            {
                Directory.CreateDirectory(certificatesFolder);
            }

            // Save file
            var fileName = $"{certificateNumber}.pdf";
            var filePath = Path.Combine(certificatesFolder, fileName);

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            return $"/Certificates/{fileName}";
        }
    }
}

namespace Select.HtmlToPdf
{
    class HtmlToPdfConverter
    {
        public HtmlToPdfConverter()
        {
        }

        internal byte[] ConvertToPdf(string html)
        {
            throw new NotImplementedException();
        }
    }
}