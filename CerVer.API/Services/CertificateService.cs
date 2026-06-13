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
            var companyName = "CerVer Authority";
            var headerSubtitle = "Digital Credentials & Verification Hub";
            var leftSignatoryName = "CerVer Registrar";
            var leftSignatoryTitle = "Authorized Officer";
            var rightSignatoryName = "CerVer Authority";
            var rightSignatoryTitle = "Lead Verifier";

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
        @page {{
            size: A4 landscape;
            margin: 0;
        }}
        body {{
            font-family: 'Arial', sans-serif;
            margin: 0;
            padding: 0;
            background: #ffffff;
            -webkit-print-color-adjust: exact;
            print-color-adjust: exact;
        }}
        .certificate {{
            width: 297mm;
            height: 210mm;
            box-sizing: border-box;
            background: white;
            border: 15px solid #d4af37;
            padding: 22px 40px;
            position: relative;
            margin: 0 auto;
            overflow: hidden;
            text-align: center;
        }}
        .certificate::before {{
            content: '';
            position: absolute;
            top: 10px;
            left: 10px;
            right: 10px;
            bottom: 10px;
            border: 2px solid #d4af37;
            pointer-events: none;
        }}
        /* Semitransparent watermark */
        .watermark {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            width: 380px;
            height: 380px;
            pointer-events: none;
            z-index: 0;
        }}
        .content-container {{
            position: relative;
            z-index: 1;
            width: 100%;
            height: 100%;
        }}
        .header {{
            margin-bottom: 10px;
        }}
        .header-title-main {{
            font-size: 24px;
            font-weight: bold;
            color: #002060;
            margin: 0;
            letter-spacing: 0.02em;
        }}
        .header-title-sub {{
            font-size: 13px;
            font-weight: bold;
            color: #4b5563;
            margin: 3px 0;
            letter-spacing: 0.15em;
            text-transform: uppercase;
        }}
        .header-subtitle {{
            font-size: 19px;
            font-weight: bold;
            color: #002060;
            margin: 3px 0;
        }}
        .certify-text {{
            font-size: 11px;
            font-style: italic;
            font-weight: bold;
            color: #002060;
            margin: 12px 0 4px 0;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }}
        .recipient-name {{
            font-size: 38px;
            font-weight: bold;
            color: #800000;
            margin: 4px 0;
            font-family: 'Arial', sans-serif;
        }}
        .eval-text {{
            font-size: 8px;
            font-weight: bold;
            color: #002060;
            line-height: 1.4;
            max-width: 820px;
            margin: 8px auto;
            text-align: center;
        }}
        .designation-title {{
            font-size: 28px;
            font-weight: bold;
            color: #002060;
            margin: 4px 0;
        }}
        .membership-id {{
            font-size: 11px;
            font-weight: bold;
            color: #800000;
            margin: 4px 0;
            text-transform: uppercase;
        }}
        .witness-text {{
            font-size: 9px;
            font-style: italic;
            font-weight: bold;
            color: #002060;
            margin: 8px 0;
            letter-spacing: 0.02em;
        }}
        
        /* 3-column Signatures & QR block */
        .sig-section {{
            margin-top: 15px;
            text-align: left;
            font-size: 0;
        }}
        .sig-col-left {{
            display: inline-block;
            width: 35%;
            font-size: 11px;
            vertical-align: bottom;
            text-align: left;
        }}
        .sig-col-center {{
            display: inline-block;
            width: 30%;
            font-size: 11px;
            vertical-align: bottom;
            text-align: center;
        }}
        .sig-col-right {{
            display: inline-block;
            width: 35%;
            font-size: 11px;
            vertical-align: bottom;
            text-align: right;
        }}
        .sig-block {{
            display: inline-block;
            text-align: center;
        }}
        .signature-cursive {{
            font-family: 'Brush Script MT', cursive;
            font-size: 26px;
            color: #002060;
            line-height: 1;
            margin-bottom: 2px;
        }}
        .signature-line {{
            width: 170px;
            border-top: 1px solid #002060;
            margin-bottom: 5px;
        }}
        .sig-name {{
            font-weight: bold;
            color: #002060;
        }}
        .sig-title {{
            color: #002060;
            font-size: 9px;
        }}

        .qr-code-img {{
            width: 55px;
            height: 55px;
            vertical-align: middle;
        }}

        /* Bottom Details Footer Panel */
        .footer-panel {{
            margin-top: 18px;
            background: #ffffff;
            border: 1.5px solid #e2e8f0;
            border-radius: 8px;
            padding: 8px 20px;
            font-size: 0;
            text-align: left;
        }}
        .footer-col {{
            display: inline-block;
            width: 33.33%;
            font-size: 11px;
            vertical-align: middle;
            box-sizing: border-box;
        }}
        .footer-label {{
            font-size: 8px;
            font-weight: bold;
            color: #002060;
            text-transform: uppercase;
            margin-bottom: 2px;
        }}
        .footer-value {{
            font-size: 12px;
            font-weight: bold;
            color: #002060;
        }}
    </style>
</head>
<body>
    <div class='certificate'>
        <!-- Semitransparent Watermark -->
        <div class='watermark'>
            <svg width='100%' height='100%' viewBox='0 0 100 100' opacity='0.04'>
                <circle cx='50' cy='50' r='45' fill='#002060' />
                <path d='M 10 30 Q 50 20 90 30' fill='none' stroke='#ffffff' stroke-width='3' />
                <path d='M 6 50 Q 50 40 94 50' fill='none' stroke='#ffffff' stroke-width='3' />
                <path d='M 10 70 Q 50 60 90 70' fill='none' stroke='#ffffff' stroke-width='3' />
            </svg>
        </div>

        <div class='content-container'>
            <!-- Styled network logo -->
            <svg width='60' height='60' viewBox='0 0 100 100' style='display: block; margin: 0 auto 5px auto;'>
                <circle cx='50' cy='50' r='45' fill='#002060' />
                <path d='M 10 30 Q 50 20 90 30' fill='none' stroke='#ffffff' stroke-width='2.5' />
                <path d='M 6 50 Q 50 40 94 50' fill='none' stroke='#ffffff' stroke-width='2.5' />
                <path d='M 10 70 Q 50 60 90 70' fill='none' stroke='#ffffff' stroke-width='2.5' />
                <path d='M 25 35 L 45 45 L 35 65' fill='none' stroke='#ff0000' stroke-width='2.5' />
                <circle cx='25' cy='35' r='5' fill='#ff0000' stroke='#ffffff' stroke-width='1.5' />
                <circle cx='45' cy='45' r='5' fill='#ff0000' stroke='#ffffff' stroke-width='1.5' />
                <circle cx='35' cy='65' r='5' fill='#ff0000' stroke='#ffffff' stroke-width='1.5' />
            </svg>

            <div class='header'>
                <div class='header-title-main'>{companyName}</div>
                <div class='header-title-sub'>{headerSubtitle}</div>
                <div class='header-subtitle'>Certificate of Membership</div>
            </div>

            <div class='certify-text'>This is to certify that</div>
            <div class='recipient-name'>{fullName}</div>
            
            <div class='eval-text'>
                HAS BEEN OFFICIALLY EVALUATED, REGISTERED, AND RECOGNIZED AS A REGISTERED AND COMPLIANT MEMBER
                WITH ALL CORRESPONDING PRIVILEGES, CREDENTIALS, AND RESPONSIBILITIES. IN ACKNOWLEDGMENT OF THESE
                QUALIFICATIONS, CERVER ADMINISTRATION CONFERS THE DESIGNATION OF:
            </div>

            <div class='designation-title'>{membershipTitle}</div>
            <div class='membership-id'>Verification Serial: {serialNumber}</div>
            
            <div class='witness-text'>
                IN WITNESS WHEREOF, THIS DIGITAL CERTIFICATE IS CRYPTOGRAPHICALLY RECORDED AND SECURED WITHIN THE CERVER CENTRAL REGISTRY.
            </div>

            <div class='sig-section'>
                <div class='sig-col-left'>
                    <div class='sig-block' style='text-align: left;'>
                        <div class='signature-cursive'>{leftSignatoryName}</div>
                        <div class='signature-line'></div>
                        <div class='sig-name'>{leftSignatoryName}</div>
                        <div class='sig-title'>{leftSignatoryTitle}</div>
                    </div>
                </div>
                <div class='sig-col-center'>
                    <img class='qr-code-img' src='data:image/png;base64,{qrCodeBase64}' />
                </div>
                <div class='sig-col-right'>
                    <div class='sig-block' style='text-align: right; display: inline-block;'>
                        <div class='signature-cursive'>{rightSignatoryName}</div>
                        <div class='signature-line' style='margin-left: auto;'></div>
                        <div class='sig-name'>{rightSignatoryName}</div>
                        <div class='sig-title'>{rightSignatoryTitle}</div>
                    </div>
                </div>
            </div>

            <div class='footer-panel'>
                <div class='footer-col' style='text-align: left;'>
                    <div class='footer-label'># Certificate No.</div>
                    <div class='footer-value'>{certificateNumber}</div>
                </div>
                <div class='footer-col' style='text-align: center;'>
                    <div class='footer-label'>📅 Issued On</div>
                    <div class='footer-value'>{issueDateFormatted}</div>
                </div>
                <div class='footer-col' style='text-align: right;'>
                    <div class='footer-label'>🛡️ Expires On</div>
                    <div class='footer-value'>{expiryDateFormatted}</div>
                </div>
            </div>
        </div>
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
                    Orientation = DinkToPdf.Orientation.Landscape,
                    PaperSize = DinkToPdf.PaperKind.A4,
                    Margins = { Top = 0, Bottom = 0, Left = 0, Right = 0 }
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