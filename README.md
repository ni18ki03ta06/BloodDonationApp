# Blood Donation Management Application

A comprehensive web application for managing blood donations, donors, blood inventory, and blood requests built with ASP.NET Core 8.0.

## 📋 Project Overview

This is a full-stack blood donation management system that includes:

- **Donor Management**: Register, manage, and track blood donors
- **Blood Inventory**: Track blood stock levels across types and RH factors
- **Appointment Booking**: Schedule and manage blood donation appointments
- **Blood Requests**: Create and manage blood transfusion requests
- **Blood Camps**: Organize and manage blood donation camps
- **Analytics**: Comprehensive analytics and reporting
- **Gamification**: Badge system and rewards for donors
- **QR Code Integration**: Digital donor cards with QR codes
- **Admin Dashboard**: Complete administrative control panel
- **API Services**: RESTful APIs with versioning (v1, v2)
- **User Notifications**: Real-time notifications via SignalR
- **Audit Logging**: Complete audit trails for all operations

## 🏗️ Architecture

### Technology Stack

**Backend:**
- .NET 8.0 (C#)
- ASP.NET Core Web Framework
- Entity Framework Core (ORM)
- SQLite / SQL Server support

**Database:**
- SQLite (default)
- SQL Server (optional)
- Entity Framework Migrations

**Key Libraries:**
- AutoMapper - Object mapping
- Serilog - Logging framework
- Swagger/Swashbuckle - API documentation
- JWT Bearer - Authentication
- QRCoder - QR code generation
- Microsoft.ML - Machine Learning recommendations
- SignalR - Real-time notifications

### Project Structure

```
BloodDonationApp/
├── Controllers/              # MVC Controllers & API endpoints
│   ├── Api/                 # RESTful API controllers
│   │   └── V1/, V2/         # API versioning
│   └── [Controller]         # MVC controllers
├── Core/
│   ├── Entities/            # Entity models
│   └── Interfaces/          # Repository interfaces
├── Data/                    # Database context & initialization
├── Application/
│   ├── DTOs/               # Data Transfer Objects
│   ├── Services/           # Business logic
│   └── Mappings/           # AutoMapper profiles
├── Infrastructure/
│   └── Repositories/       # Data access layer
├── Views/                  # Razor views
├── wwwroot/               # Static files (CSS, JS)
├── Migrations/            # EF Core migrations
└── Services/              # Supporting services
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or Visual Studio Code
- Git
- SQL Server / SQLite (pre-configured)

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/BloodDonationApp.git
   cd BloodDonationApp/BloodDonationApp
   ```

2. **Restore NuGet packages:**
   ```bash
   dotnet restore
   ```

3. **Apply database migrations (if needed):**
   ```bash
   dotnet ef database update
   ```

4. **Run the application:**
   ```bash
   dotnet run
   ```

5. **Access the application:**
   - Open browser: `http://localhost:5000` (or configured port)
   - API Documentation: `http://localhost:5000/swagger`

## 📊 Key Features

### For Donors
- User registration and profile management
- Book donation appointments
- View donation history
- Track badges and achievements
- Digital donor card with QR code
- Leaderboard rankings
- Notification center
- Blood request search

### For Admins
- Comprehensive dashboard with analytics
- Donor management and verification
- Blood inventory tracking
- Appointment management
- Blood request processing
- Audit logs and reports
- Camp management
- Feedback review
- User activity monitoring

### For API Users
- RESTful API endpoints
- Multiple API versions (v1, v2)
- JWT authentication
- API key support
- Comprehensive documentation via Swagger

## 🔐 Authentication & Security

- JWT Token-based authentication
- API Key validation via middleware
- Password hashing
- Role-based access control (RBAC)
- Audit logging for sensitive operations
- Exception handling middleware

## 📱 API Endpoints

### Base URL: `/api`

**Donors:**
- `GET /v1/donors` - List all donors
- `POST /v1/donors` - Create new donor
- `GET /v1/donors/{id}` - Get donor details
- `PUT /v1/donors/{id}` - Update donor
- `DELETE /v1/donors/{id}` - Delete donor

**Blood Inventory:**
- `GET /v2/inventory` - List inventory (v2)
- `POST /inventory` - Add blood stock
- `GET /inventory/{type}` - Get by blood type

**Blood Requests:**
- `GET /requests` - List requests
- `POST /requests` - Create request
- `PUT /requests/{id}` - Update request status

**Appointments:**
- `GET /appointments` - List appointments
- `POST /appointments` - Book appointment
- `PUT /appointments/{id}/status` - Update status

## 📈 Database Schema

Key tables:
- **Donors** - Donor information
- **BloodInventory** - Blood stock management
- **Appointments** - Appointment records
- **BloodRequests** - Transfusion requests
- **BloodCamps** - Campaign information
- **DonationRecords** - Historical donations
- **Badges** - Achievement badges
- **Notifications** - User notifications
- **AuditLogs** - Activity logs
- **Feedback** - User feedback

## 🧪 Testing

Run unit tests (when available):
```bash
dotnet test
```

## 📝 Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=BloodDonation.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Environment Variables
- `ASPNETCORE_ENVIRONMENT` - Development/Production
- `ConnectionString` - Database connection string
- `JwtSecret` - JWT signing secret

## 🐳 Docker Support

Build Docker image:
```bash
docker build -t blood-donation-app .
docker run -p 5000:80 blood-donation-app
```

## 📝 License

This project is licensed under the MIT License - see LICENSE file for details.

## 👥 Contributors

- Your Name / Team Members

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📧 Contact

For questions or support, please contact: [your-email@example.com]

## 🔗 Resources

- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Swagger Documentation](https://swagger.io/)

---
# Donora - Blood Donation & Inventory Management System

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](https://opensource.org/licenses/MIT)
[![ML.NET](https://img.shields.io/badge/Machine%20Learning-ML.NET-orange?style=for-the-badge)](https://dotnet.microsoft.com/en-us/apps/machinelearning-ai/ml-dotnet)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue?style=for-the-badge)]()

**Donora** is a premium, state-of-the-art Blood Donation and Inventory Management application built using **ASP.NET Core 8.0**. Designed as a digital bridge between voluntary blood donors, patients in urgent need, and healthcare institutions, Donora provides a complete dashboard-driven portal experience, gamified achievements, real-time matching via interactive proximity maps, and predictive AI-powered stock forecasting.

---

**Last Updated:** June 2, 2026

---
📸 Screenshots

