# 🏆 CerVer.API - Certificate Generation & Verification System

**A production-ready REST API for managing digital certificates with QR code verification**

[![Live API](https://img.shields.io/badge/Live_API-Azure-blue?style=for-the-badge&logo=microsoftazure)](https://cerver-api-ehb0hnc4fvdnfkc0.eastasia-01.azurewebsites.net/swagger)
[![Swagger Docs](https://img.shields.io/badge/Swagger_Docs-85EA2D?style=for-the-badge&logo=swagger)](https://cerver-api-ehb0hnc4fvdnfkc0.eastasia-01.azurewebsites.net/swagger)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

---

## 📋 Overview

CerVer.API is a complete backend solution for certificate management systems. Organizations can issue digital certificates, manage membership requests, and provide public verification through QR codes.

**Live API Documentation:** [https://cerver-api-ehb0hnc4fvdnfkc0.eastasia-01.azurewebsites.net/swagger](https://cerver-api-ehb0hnc4fvdnfkc0.eastasia-01.azurewebsites.net/swagger)

**Frontend Repository:** [cerver-frontend](https://github.com/web3doctuur-cloud/cerver-frontend)

---

## ✨ Features

### 🔐 Authentication & Authorization
| Feature | Description |
|---------|-------------|
| JWT Authentication | Secure token-based authentication |
| Role-Based Access | Admin and User roles with different permissions |
| Password Hashing | ASP.NET Core Identity for secure password storage |
| Token Expiry | Configurable token expiration (60 minutes default) |

### 📝 Membership Management
| Feature | Description |
|---------|-------------|
| CRUD Operations | Create, Read, Update, Delete membership types |
| Active/Inactive Status | Toggle membership visibility |
| Benefits & Requirements | Store detailed membership information |

### 📋 Request Workflow
| Feature | Description |
|---------|-------------|
| User Requests | Submit membership requests with file uploads |
| Admin Approval | Approve or reject requests with comments |
| Auto-Certificate | Certificates auto-generate upon approval |
| Email Notifications | Automatic emails at each workflow stage |

### 🏆 Certificate Generation
| Feature | Description |
|---------|-------------|
| PDF Generation | Professional certificate design with DinkToPdf |
| QR Code Generation | Unique QR codes for instant verification |
| Digital Signatures | President signature and corporate seal |
| Expiry Dates | 2-year validity period with expiry tracking |

### ✅ Verification System
| Feature | Description |
|---------|-------------|
| Public API | Anyone can verify certificates without login |
| QR Code Support | Scan QR codes for instant verification |
| Real-Time Status | Active, Expired, or Revoked status |

### 📊 Analytics Dashboard
| Feature | Description |
|---------|-------------|
| Real-Time Stats | Memberships, requests, certificates, users |
| Popularity Metrics | Most requested memberships |
| Activity Feed | Recent requests, approvals, certificates |
| Performance Metrics | Average processing time, peak hours |

---

## 🛠️ Tech Stack

| Category | Technology | Version |
|----------|------------|---------|
| **Framework** | ASP.NET Core Web API | 10.0 |
| **Database** | SQL Server / Azure SQL | - |
| **ORM** | Entity Framework Core | 10.0 |
| **Authentication** | JWT + ASP.NET Core Identity | 10.0 |
| **PDF Generation** | DinkToPdf | 1.0.8 |
| **QR Code** | QRCoder | 1.8.0 |
| **Email** | MailKit | 4.17.0 |
| **API Documentation** | Swashbuckle / Swagger | 6.5.0 |
| **Deployment** | Microsoft Azure | - |

---

## 📁 Project Structure
CerVer.API/
├── Controllers/ # API endpoints (29 total)
│ ├── AuthController.cs # Registration, Login
│ ├── MembershipsController.cs # Membership CRUD
│ ├── MembershipRequestsController.cs # Request workflow
│ ├── CertificatesController.cs # Certificate management
│ ├── AdminController.cs # Admin operations
│ └── AnalyticsController.cs # Dashboard analytics
├── Models/ # Data entities
│ ├── Membership.cs # Membership type
│ ├── MembershipRequest.cs # User requests
│ ├── Certificate.cs # Issued certificates
│ ├── CreateRequestModel.cs # Request DTO
│ └── RejectRequestModel.cs # Rejection DTO
├── Services/ # Business logic
│ ├── CertificateService.cs # PDF + QR generation
│ ├── FileUploadService.cs # File handling
│ └── EmailService.cs # Email notifications
├── Data/ # Database context
│ └── ApplicationDbContext.cs # EF Core DbContext
├── Migrations/ # EF Core migrations
├── wwwroot/ # Static files
│ ├── verify.html # Public verification page
│ └── Certificates/ # Generated PDFs
└── Program.cs # Startup configuration
 
---

## 🚀 Getting Started

### Prerequisites

| Requirement | Version |
|-------------|---------|
| Visual Studio | 2022+ |
| .NET SDK | 10.0+ |
| SQL Server | LocalDB / Azure SQL |

### Installation

```bash
# Clone the repository
git clone https://github.com/web3doctuur-cloud/CerVerApi.git

# Navigate to project directory
cd CerVerApi/CerVer.API

# Restore NuGet packages
dotnet restore

# Update database (LocalDB)
dotnet ef database update

# Run the application
dotnet run
📡 API Endpoints (29 Total)
Authentication
Method	Endpoint	Description	Auth
POST	/api/Auth/register	Register new user	None
POST	/api/Auth/login	Login and get JWT token	None
Memberships
Method	Endpoint	Description	Auth
GET	/api/Memberships	Get active memberships	None
GET	/api/Memberships/all	Get all memberships	Admin
GET	/api/Memberships/{id}	Get membership by ID	None
POST	/api/Memberships	Create membership	Admin
PUT	/api/Memberships/{id}	Update membership	Admin
DELETE	/api/Memberships/{id}	Delete membership	Admin
Membership Requests
Method	Endpoint	Description	Auth
GET	/api/MembershipRequests	Get all requests	Admin
GET	/api/MembershipRequests/my	Get user's requests	User
GET	/api/MembershipRequests/pending	Get pending requests	Admin
POST	/api/MembershipRequests	Submit request	User
POST	/api/MembershipRequests/{id}/approve	Approve request	Admin
POST	/api/MembershipRequests/{id}/reject	Reject request	Admin
POST	/api/MembershipRequests/{id}/generate-certificate	Generate certificate	Admin
Certificates
Method	Endpoint	Description	Auth
GET	/api/Certificates/my	Get user's certificates	User
GET	/api/Certificates/download/{certNumber}	Download PDF	User
GET	/api/Certificates/verify/{certNumber}	Public verification	None
DELETE	/api/Certificates/revoke/{certNumber}	Revoke certificate	Admin
 

<img width="1090" height="812" alt="image" src="https://github.com/user-attachments/assets/78fa917a-b5a3-4093-9785-87c2ebb68cbc" />
🚀 Deployment
Deploy to Azure App Service
# Publish locally
dotnet publish -c Release -o ./publish

# Deploy via Azure CLI
az webapp deployment source config-zip \
  --resource-group CerVer-RG \
  --name cerver-api \
  --src ./publish.zip
🧪** Testing**
Using Swagger UI
Using Postman

# Relationships
AspNetUsers (1) ----< (many) MembershipRequests
Memberships (1) ----< (many) MembershipRequests
MembershipRequests (1) ----< (1) Certificates

.

👨‍💻 Author
Yusuf Rodiah Hadizah

Platform	Link
GitHub	@web3doctuur-cloud
Email	hadizahrodiah@gmail.com
Frontend Demo	cerver-frontend.vercel.app

🙏 Acknowledgments
Microsoft for ASP.NET Core and Azure free tier

Swashbuckle for Swagger integration

DinkToPdf for PDF generation

QRCoder for QR code generation

MailKit for email handling

Azure and GitHub for deployment

⭐ Show Your Support
If this API helped you learn or inspired your project, please give it a ⭐!

https://img.shields.io/github/stars/web3doctuur-cloud/CerVerApi?style=social
Built with ❤️ by Yusuf Rodiah Hadizah

