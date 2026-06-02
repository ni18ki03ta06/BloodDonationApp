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

For questions or support, please contact: [nikitabansod22@gmail.com]

**Last Updated:** June 2, 2026



## 🗺️ Application Walkthrough & Screenshots

To ensure maximum readability, the screenshots are presented in logical user-flow sequence: **Guest Experience & Registration** ➡️ **Donor Portal & Gamification** ➡️ **Admin Panel & ML Analytics**.

---

### 🌐 Guest Portal & Informational Pages

#### 1. Platform Landing Page
![Platform Landing Page](images/01-landing-page.png)
*The entry screen of the application offering split glassmorphic pathways to either the Donor Portal or Admin Portal, paired with the core platform mission.*

#### 2. Public Blood Stock - Availability Cards
![Public Blood Stock - Availability Cards](images/02-public-blood-stock-upper.png)
*Real-time overview of the public stock levels categorized into Critical (<5 units), Low (5-15 units), and Good (>15 units) for O+, O-, A+, and A- blood types.*

#### 3. Public Blood Stock - Actions & Request Shortcut
![Public Blood Stock - Actions & Request Shortcut](images/03-public-blood-stock-lower.png)
*Lower half of the public stock levels showing B+, B-, AB+, and AB- blood types with rapid action shortcuts to register as a donor or submit a patient request.*

#### 4. About Us - Core Values
![About Us - Core Values](images/04-about-us-upper.png)
*Explains the core mission of the platform, detailing the dedication to safety first, community benefit, and direct connection channels.*

#### 5. About Us - The Donation Journey & Stats
![About Us - The Donation Journey & Stats](images/05-about-us-lower.png)
*Walks potential donors through the 4 core steps of the donation journey, backed by platform statistics such as voluntary donor counts and lives impacted.*

#### 6. Contact Us & Helpdesk
![Contact Us & Helpdesk](images/06-contact-us.png)
*A public form for user inquiries, suggestions, and feedback, alongside office location markers and the emergency hotline details.*

#### 7. Donor Registration - Step 1
![Donor Registration - Step 1](images/07-donor-registration-upper.png)
*The primary portion of the registration form collecting basic personal details, blood group type, state, city, and account credentials.*

#### 8. Donor Registration - Step 2
![Donor Registration - Step 2](images/08-donor-registration-lower.png)
*The secondary portion of the registration form containing address fields, age, gender validation, last donation date, profile picture upload, and terms consent.*

#### 9. Donor Portal Login
![Donor Portal Login](images/09-donor-login.png)
*Secure authentication screen for registered donors to log in and manage their profiles.*

#### 10. Admin Portal Login
![Admin Portal Login](images/10-admin-login.png)
*Restricted admin login gate for system administrators and managers.*

---

### 🩸 Donor Portal Experience

#### 11. Donor Dashboard - Welcome & KPI Cards
![Donor Dashboard - Welcome & KPI Cards](images/11-donor-dashboard-upper.png)
*The main donor hub displaying the logged-in donor's current tier (e.g. Regular Hero), availability toggle, blood type verification, next eligibility timer, and critical emergency banners.*

#### 12. Donor Dashboard - Stock Levels & Guidelines
![Donor Dashboard - Stock Levels & Guidelines](images/12-donor-dashboard-lower.png)
*The bottom half of the donor dashboard providing quick-action links (profile edits, blood requests) and crucial medical donation guidelines.*

#### 13. My Notifications
![My Notifications](images/13-donor-notifications.png)
*Real-time alerts center informing donors of urgent matching blood requests in their city or updates on their eligibility verification status.*

#### 14. Donor Profile & Milestones
![Donor Profile & Milestones](images/14-donor-profile.png)
*Detailed profile page displaying donor statistics, personal records, level milestones, and unlocked contribution achievement timelines.*

#### 15. Toggle Donation Availability
![Toggle Donation Availability](images/15-donor-toggle-availability.png)
*Donor privacy toggle allowing them to instantly update their search visibility status for emergency match-making queries.*

#### 16. My Donation Appointments
![My Donation Appointments](images/16-donor-appointment-calendar.png)
*Interactive calendar system allowing donors to schedule future donations, choose scheduling slots, and review scheduling histories.*

#### 17. Submit a Blood Request
![Submit a Blood Request](images/17-donor-blood-request.png)
*Form allowing authenticated donors to submit request tickets for family members or patients, specifying units required, urgency, and anonymity status.*

#### 18. Search Blood Donors (Directory)
![Search Blood Donors (Directory)](images/18-donor-directory-search.png)
*A searchable directory of active voluntary donors filters by city, blood type, age range, and verification status with direct options to submit requests to specific donors.*

#### 19. Proximity Map Finder - Proximity View
![Proximity Map Finder - Proximity View](images/19-donor-map-standard.png)
*Interactive Leaflet proximity map locating compatible donors and upcoming donation camps in the donor's vicinity.*

#### 20. Proximity Map Finder - Contact Info Detail
![Proximity Map Finder - Contact Info Detail](images/41-donor-map-details.png)
*Expanded map view showing specific distances (e.g. 3.1 km) and revealing verified contact details (phone, email) for matched donors.*

#### 21. Blood Donation Camps
![Blood Donation Camps](images/20-donor-blood-camps.png)
*Upcoming donation camp calendar showcasing host organizations, locations, slot availability, and registration options.*

#### 22. Donation History & Earned Badges
![Donation History & Earned Badges](images/21-donor-history-badges.png)
*Showcases gamified donor profile rewards including unlocked badges (First Drop, Life Saver, Emergency Responder) and personal donation trends.*

#### 23. Donation Records & Certification
![Donation Records & Certification](images/22-donor-history-records.png)
*Tracks completed historical donation entries with dates, units donated, hospital names, status validations, and printable donation certificates.*

#### 24. Donor Digital ID Card
![Donor Digital ID Card](images/23-donor-digital-id.png)
*The verified digital identification pass containing the donor's verification ID, QR code, state, and total donation count.*

#### 25. Printable Digital Donor Card
![Printable Digital Donor Card](images/24-donor-card-qr-details.png)
*A specialized overlay card view displaying donor rankings (Level 4), lifetime points (950 pts), and a printable card download option.*

#### 26. Leaderboard - Top Contributors
![Leaderboard - Top Contributors](images/25-donor-leaderboard.png)
*Ranks the top donors in the Donora community based on donation counts, streaks, level tiers, badges, and points.*

#### 27. Achievements & Rewards - Level Details
![Achievements & Rewards - Level Details](images/26-donor-achievements-rewards.png)
*Displays points breakdown (XP Progress) for gamification tiers (e.g. Gold Lifesaver) and tracks spendable reward points.*

#### 28. Milestone Rewards Store
![Milestone Rewards Store](images/27-donor-rewards-store.png)
*A store interface where donors can spend their reward points to redeem milestone gifts (e.g. custom T-shirts, First Aid kits).*

---

### 🛡️ Administrative Portal

#### 29. Admin Dashboard - KPI Summary
![Admin Dashboard - KPI Summary](images/28-admin-dashboard-overview.png)
*The central administrative hub showing critical telemetry (Emergency cases, Pending requests, Approved requests, Total registered donors) and live inventory stocks.*

#### 30. Admin Dashboard - AI Recommendations & Insights
![Admin Dashboard - AI Recommendations & Insights](images/29-admin-ai-matching-analytics.png)
*Visualizes real-time AI-powered matching recommendations to instantly suggest best-fit nearby donors for pending critical patient requests, accompanied by request charts.*

#### 31. Admin Dashboard - Recent Activity Tables
![Admin Dashboard - Recent Activity Tables](images/30-admin-recent-activity.png)
*Summary tables displayed at the bottom of the admin dashboard for quick access to the latest registered donors and pending blood requests.*

#### 32. Manage Blood Requests
![Manage Blood Requests](images/31-admin-manage-requests.png)
*Comprehensive request queue displaying patient details, required date, urgency, and status filters, with direct matching donor options.*

#### 33. User Accounts Management
![User Accounts Management](images/32-admin-user-management.png)
*A centralized roster of all registered user and administrator accounts, displaying details, roles, credentials, and real-time status.*

#### 34. Advanced Analytics Dashboard - KPIs & Data Exports
![Advanced Analytics Dashboard - KPIs & Data Exports](images/33-admin-advanced-analytics-upper.png)
*Displays advanced platform analytics with instant data exporters for Donors, Requests, and Histories in CSV, Excel, and PDF formats.*

#### 35. Advanced Analytics Dashboard - Visualizations
![Advanced Analytics Dashboard - Visualizations](images/34-admin-advanced-analytics-lower.png)
*Detailed visual trend charts tracking platform activity, donor blood group distributions, and donor badge tier distributions.*

#### 36. ML.NET Demand Forecasting - Stock Shortage Risk Predictions
![ML.NET Demand Forecasting - Stock Shortage Risk Predictions](images/35-admin-demand-forecasting-upper.png)
*State-of-the-art predictive module leveraging ML.NET to calculate stock shortage risks (Next 4 Weeks) and output low stock alerts.*

#### 37. ML.NET Demand Forecasting - Inventory vs. Predicted Demand
![ML.NET Demand Forecasting - Inventory vs. Predicted Demand](images/36-admin-demand-forecasting-charts.png)
*Machine learning analytics visualization charting current stocks against next week's predicted demand and recommended stock levels.*

#### 38. ML.NET Demand Forecasting - Insights & Performance
![ML.NET Demand Forecasting - Insights & Performance](images/37-admin-demand-forecasting-insights.png)
*Deep-dive insights revealing monthly demand volumes, high-demand blood groups (ranked by forecast size), and monthly performance metrics.*

#### 39. Platform Stats & Visualizations
![Platform Stats & Visualizations](images/38-admin-platform-visualizations.png)
*Telemetry graphs showing donation timelines, blood type spreads, request fulfillment rates, and donor availability dynamics.*

#### 40. Blood Inventory Stock Management
![Blood Inventory Stock Management](images/39-admin-inventory-stock-management.png)
*A dedicated ledger allowing admins to view available vs. reserved units and manually adjust physical stock counts for all 8 blood groups.*

#### 41. Appointments Manager
![Appointments Manager](images/40-admin-appointments-manager.png)
*Renders a scheduling calendar and appointment queue tracking pending, approved, and completed donation appointments.*

#### 42. Manage System Administrators
![Manage System Administrators](images/42-admin-manage-administrators.png)
*An access-restricted settings view specifically for Super Administrators to add, edit, or remove staff and administrative accounts.*

#### 43. System Audit Log
![System Audit Log](images/43-admin-system-audit-log.png)
*A security audit trail logging system events, actors, actions, and altered entities for transparency.*

#### 44. User Feedback & Helpdesk
![User Feedback & Helpdesk](images/44-admin-user-feedback.png)
*The administrative support queue tracking incoming feedback, questions, or issues submitted by public users.*

#### 45. QR Scan History Log
![QR Scan History Log](images/45-admin-qr-scan-history.png)
*Audit trail recording every instance of digital donor ID card scans at donation camps for security and validation.*

#### 46. Portal Settings (Configurations)
![Portal Settings (Configurations)](images/46-admin-portal-settings.png)
*Platform-wide configuration forms allowing admins to manage the site name, notification emails, and toggle automatic emergency broadcast parameters.*
---
**Last Updated:** June 2, 2026
---
