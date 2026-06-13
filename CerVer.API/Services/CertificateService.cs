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
        @page {{
            size: A4 landscape;
            margin: 0;
        }}
        body {{
            font-family: 'Georgia', 'Times New Roman', serif;
            margin: 0;
            padding: 0;
            background: #ffffff;
            -webkit-print-color-adjust: exact;
            print-color-adjust: exact;
        }}
        .cert-container {{
            width: 297mm;
            height: 210mm;
            box-sizing: border-box;
            padding: 2.5rem;
            position: relative;
            background-color: #ffffff;
            overflow: hidden;
        }}
        /* Dual Gold Border Frame */
        .cert-border-outer {{
            width: 100%;
            height: 100%;
            border: 10px solid #d97706;
            box-sizing: border-box;
            padding: 8px;
            position: relative;
        }}
        .cert-border-inner {{
            width: 100%;
            height: 100%;
            border: 2px solid #b45309;
            box-sizing: border-box;
            padding: 2.5rem;
            text-align: center;
        }}
        /* Classical Corner Ornaments */
        .corner {{
            position: absolute;
            width: 30px;
            height: 30px;
            border-color: #b45309;
            border-style: solid;
        }}
        .top-left {{ top: 12px; left: 12px; border-width: 3px 0 0 3px; }}
        .top-right {{ top: 12px; right: 12px; border-width: 3px 3px 0 0; }}
        .bottom-left {{ bottom: 12px; left: 12px; border-width: 0 0 3px 3px; }}
        .bottom-right {{ bottom: 12px; right: 12px; border-width: 0 3px 3px 0; }}

        .cert-header {{
            margin-bottom: 1.5rem;
        }}
        .company-name {{
            font-family: 'Arial', sans-serif;
            font-size: 13px;
            font-weight: bold;
            letter-spacing: 0.35em;
            color: #4b5563;
            text-transform: uppercase;
        }}
        .cert-title {{
            font-size: 38px;
            color: #d97706;
            margin: 15px 0 5px 0;
            font-weight: normal;
        }}
        .cert-subtitle {{
            font-family: 'Arial', sans-serif;
            font-size: 14px;
            color: #6b7280;
            margin: 0;
        }}
        .recipient-prefix {{
            font-size: 15px;
            color: #4b5563;
            margin: 1.5rem 0 0.5rem 0;
            font-style: italic;
        }}
        .recipient-name {{
            font-size: 40px;
            font-weight: bold;
            color: #111827;
            margin: 5px 0;
            border-bottom: 2px solid #f59e0b;
            display: inline-block;
            padding-bottom: 5px;
            min-width: 400px;
        }}
        .membership-text {{
            font-size: 15px;
            color: #4b5563;
            margin: 1rem 0;
        }}
        .membership-badge {{
            display: inline-block;
            background-color: #fef3c7;
            border: 1px solid #f59e0b;
            color: #b45309;
            padding: 8px 24px;
            font-size: 16px;
            font-weight: bold;
            font-family: 'Arial', sans-serif;
            border-radius: 4px;
        }}
        
        /* Grid Details (Inline block columns for DinkToPdf compatibility) */
        .cert-details {{
            margin: 2.2rem 0;
            text-align: left;
            font-size: 0;
        }}
        .detail-col {{
            display: inline-block;
            width: 25%;
            font-size: 12px;
            vertical-align: top;
            box-sizing: border-box;
            padding: 0 10px;
        }}
        .detail-label {{
            font-family: 'Arial', sans-serif;
            text-transform: uppercase;
            font-size: 9px;
            letter-spacing: 0.1em;
            color: #9ca3af;
            margin-bottom: 4px;
            font-weight: bold;
        }}
        .detail-value {{
            color: #1f2937;
            font-weight: bold;
            font-family: 'Arial', sans-serif;
            font-size: 12px;
        }}
        .detail-value-mono {{
            color: #1f2937;
            font-weight: bold;
            font-family: monospace;
            font-size: 13px;
        }}

        /* Bottom Section alignment */
        .bottom-section {{
            margin-top: 1.5rem;
            text-align: left;
            font-size: 0;
        }}
        .bottom-col-left {{
            display: inline-block;
            width: 33%;
            font-size: 13px;
            vertical-align: bottom;
        }}
        .bottom-col-center {{
            display: inline-block;
            width: 34%;
            font-size: 13px;
            text-align: center;
            vertical-align: bottom;
        }}
        .bottom-col-right {{
            display: inline-block;
            width: 33%;
            font-size: 13px;
            text-align: right;
            vertical-align: bottom;
        }}

        /* Signatures and Seals */
        .signature-block {{
            display: inline-block;
            text-align: center;
        }}
        .signature-line {{
            width: 180px;
            border-top: 1.5px solid #1f2937;
            margin-bottom: 8px;
        }}
        .signature-name {{
            font-weight: bold;
            color: #111827;
        }}
        .signature-title {{
            font-family: 'Arial', sans-serif;
            font-size: 11px;
            color: #6b7280;
            margin-top: 2px;
        }}

        /* QR Wrapper */
        .qr-wrapper {{
            display: inline-block;
            text-align: center;
        }}
        .qr-code-img {{
            width: 80px;
            height: 80px;
            border: 1px solid #e5e7eb;
            padding: 4px;
            background: #ffffff;
        }}
        .verification-text {{
            font-family: 'Arial', sans-serif;
            font-size: 8px;
            color: #9ca3af;
            margin-top: 6px;
            word-break: break-all;
        }}
    </style>
</head>
<body>
    <div class='cert-container'>
        <div class='cert-border-outer'>
            <div class='top-left corner'></div>
            <div class='top-right corner'></div>
            <div class='bottom-left corner'></div>
            <div class='bottom-right corner'></div>
            <div class='cert-border-inner'>
                <div class='cert-header'>
                    <div class='company-name'>{companyName}</div>
                    <h1 class='cert-title'>Certificate of Membership</h1>
                    <p class='cert-subtitle'>OFFICIAL CREDENTIAL OF COMPLIANCE</p>
                </div>

                <div class='recipient-prefix'>This is proudly presented to</div>
                <div class='recipient-name'>{fullName}</div>
                
                <div class='membership-text'>for approved and recognized membership status in</div>
                <div>
                    <div class='membership-badge'>{membershipTitle}</div>
                </div>

                <div class='cert-details'>
                    <div class='detail-col'>
                        <div class='detail-label'>Certificate Number</div>
                        <div class='detail-value-mono'>{certificateNumber}</div>
                    </div>
                    <div class='detail-col'>
                        <div class='detail-label'>Serial Number</div>
                        <div class='detail-value-mono'>{serialNumber}</div>
                    </div>
                    <div class='detail-col'>
                        <div class='detail-label'>Issue Date</div>
                        <div class='detail-value'>{issueDateFormatted}</div>
                    </div>
                    <div class='detail-col'>
                        <div class='detail-label'>Expiry Date</div>
                        <div class='detail-value'>{expiryDateFormatted}</div>
                    </div>
                </div>

                <div class='bottom-section'>
                    <div class='bottom-col-left'>
                        <div class='signature-block' style='text-align: left;'>
                            <div class='signature-line'></div>
                            <div class='signature-name'>{presidentName}</div>
                            <div class='signature-title'>{presidentTitle}</div>
                        </div>
                    </div>
                    <div class='bottom-col-center'>
                        <div class='qr-wrapper'>
                            <img class='qr-code-img' src='data:image/png;base64,{qrCodeBase64}' />
                            <div class='verification-text'>Verification Link:<br/>{baseUrl}/verify/{certificateNumber}</div>
                        </div>
                    </div>
                    <div class='bottom-col-right'>
                        <div class='signature-block' style='text-align: right; display: inline-block;'>
                            <div class='signature-line' style='margin-left: auto;'></div>
                            <div class='signature-name'>{companyName}</div>
                            <div class='signature-title'>Corporate Seal & Verification</div>
                        </div>
                    </div>
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