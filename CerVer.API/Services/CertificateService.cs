using DinkToPdf;
using DinkToPdf.Contracts;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace CerVer.API.Services
{
    public class CertificateService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IConverter _converter;

        public CertificateService(IWebHostEnvironment environment, IConfiguration configuration, IConverter converter)
        {
            _environment = environment;
            _configuration = configuration;
            _converter = converter;
        }

        public string GenerateCertificateNumber()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random();
            var sequence = random.Next(10000, 99999).ToString();
            return $"CERT-{date}-{sequence}";
        }

        public string GenerateSerialNumber()
        {
            return Guid.NewGuid().ToString().ToUpper();
        }

        public string GenerateQRCode(string verificationUrl)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrCodeData))
                {
                    using (var qrBitmap = qrCode.GetGraphic(20))
                    {
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

        public string GenerateCertificateHtml(
            string fullName,
            string membershipTitle,
            string certificateNumber,
            string serialNumber,
            DateTime issueDate,
            DateTime expiryDate,
            string qrCodeBase64)
        {
            var companyName = "CerVer";
            var presidentName = "Dr. John Smith";
            var presidentTitle = "President";

            var issueDateFormatted = issueDate.ToString("MMMM dd, yyyy");
            var expiryDateFormatted = expiryDate.ToString("MMMM dd, yyyy");

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Certificate of Membership</title>
    <style>
        body {{
            font-family: 'Times New Roman', serif;
            margin: 0;
            padding: 20px;
            background: #f0f0f0;
        }}
        .certificate {{
            max-width: 900px;
            margin: 0 auto;
            background: white;
            border: 15px solid #d4af37;
            padding: 40px;
            position: relative;
        }}
        .certificate::before {{
            content: '';
            position: absolute;
            top: 15px;
            left: 15px;
            right: 15px;
            bottom: 15px;
            border: 2px solid #d4af37;
        }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .company-name {{ font-size: 36px; font-weight: bold; color: #2c3e50; }}
        .certificate-title {{ font-size: 48px; color: #d4af37; margin: 20px 0; }}
        .recipient-name {{ font-size: 42px; font-weight: bold; text-align: center; margin: 30px 0; }}
        .membership-badge {{ background: #d4af37; padding: 5px 20px; border-radius: 25px; display: inline-block; }}
        .details {{ margin: 30px 0; display: flex; justify-content: space-between; flex-wrap: wrap; }}
        .detail-item {{ width: 45%; margin: 10px 0; }}
        .qr-section {{ display: flex; justify-content: space-between; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; }}
        .qr-code img {{ width: 100px; height: 100px; }}
        .signatures {{ margin-top: 40px; display: flex; justify-content: space-between; }}
        .signature-line {{ width: 200px; border-top: 1px solid #333; margin: 10px 0; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #999; }}
    </style>
</head>
<body>
    <div class='certificate'>
        <div class='header'>
            <div class='company-name'>{companyName}</div>
            <div class='certificate-title'>Certificate of Membership</div>
        </div>
        <div class='recipient-name'>{fullName}</div>
        <div style='text-align: center;'>
            <div class='membership-badge'>{membershipTitle}</div>
        </div>
        <div class='details'>
            <div class='detail-item'><strong>Certificate Number:</strong><br/>{certificateNumber}</div>
            <div class='detail-item'><strong>Serial Number:</strong><br/>{serialNumber}</div>
            <div class='detail-item'><strong>Issue Date:</strong><br/>{issueDateFormatted}</div>
            <div class='detail-item'><strong>Expiry Date:</strong><br/>{expiryDateFormatted}</div>
        </div>
        <div class='qr-section'>
            <div class='qr-code'><img src='data:image/png;base64,{qrCodeBase64}' /></div>
            <div>Verify at: {baseUrl}/verify/{certificateNumber}</div>
        </div>
        <div class='signatures'>
            <div><div class='signature-line'></div>{presidentName}<br/>{presidentTitle}</div>
            <div><div class='signature-line'></div>{companyName}<br/>Corporate Seal</div>
        </div>
        <div class='footer'>This certificate is the property of {companyName}</div>
    </div>
</body>
</html>";
        }

        public async Task<byte[]> GeneratePdfFromHtml(string html)
        {
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = DinkToPdf.ColorMode.Color,
                    Orientation = DinkToPdf.Orientation.Portrait,
                    PaperSize = DinkToPdf.PaperKind.A4,
                },
                Objects = {
                    new ObjectSettings() {
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8" },
                    }
                }
            };

            byte[] pdf = _converter.Convert(doc);
            return await Task.FromResult(pdf);
        }

        public async Task<string> SaveCertificatePdf(byte[] pdfBytes, string certificateNumber)
        {
            var certificatesFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "Certificates");

            if (!Directory.Exists(certificatesFolder))
            {
                Directory.CreateDirectory(certificatesFolder);
            }

            var fileName = $"{certificateNumber}.pdf";
            var filePath = Path.Combine(certificatesFolder, fileName);

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            return $"/Certificates/{fileName}";
        }
    }
}