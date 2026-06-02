# 📚 Blood Donation App - Project Understanding Guide

## 🎯 Project Overview

The Blood Donation Application is a **full-stack ASP.NET Core 8.0 web application** designed to manage and facilitate blood donation operations.

### Key Stats:
- **Technology**: ASP.NET Core 8.0 with MVC Architecture
- **Database**: SQLite (with SQL Server support)
- **Main Language**: C#
- **Type**: Web Application (MVC + API)
- **Features**: 30+ major features including gamification, analytics, and real-time notifications

---

## 🏛️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                        │
│  (Views, Controllers, API Endpoints)                         │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                          │
│  (DTOs, Services, Mappings, Business Logic)                  │
├─────────────────────────────────────────────────────────────┤
│                    Domain Layer                               │
│  (Entities, Interfaces, Core Business Rules)                 │
├─────────────────────────────────────────────────────────────┤
│                 Infrastructure Layer                          │
│  (Repositories, Database Context, Data Access)               │
├─────────────────────────────────────────────────────────────┤
│                    Database Layer                             │
│  (SQLite/SQL Server - Entity Framework Core)                 │
└─────────────────────────────────────────────────────────────┘
```

---

## 📁 Detailed Folder Structure & Purpose

### 1. **Controllers/** - Request Handling Layer
**Purpose**: Handle HTTP requests and responses

```
Controllers/
├── AccountController.cs          # Login, registration, auth
├── AdminController.cs             # Admin dashboard & management
├── DonorController.cs             # Donor operations
├── BloodRequestController.cs       # Blood request management
├── CampController.cs              # Donation camp management
├── InventoryController.cs         # Blood inventory management
├── MapController.cs               # Location/map features
└── Api/
    ├── V1/                        # API version 1
    │   ├── ApiAuthController.cs
    │   ├── ApiDonorV1Controller.cs
    │   ├── ApiInventoryV1Controller.cs
    │   └── ApiRequestV1Controller.cs
    └── V2/                        # API version 2
        └── ApiInventoryV2Controller.cs
```

**Key Concept**: Each controller handles specific domain operations
- `[Route("api/v1/donors")]` - Routes incoming requests
- `public IActionResult Create(DonorDto donor)` - Action methods

---

### 2. **Views/** - User Interface Layer
**Purpose**: HTML/Razor templates for rendering web pages

```
Views/
├── Account/                       # Login, registration
├── Admin/                         # Admin dashboard pages
│   ├── Index.cshtml              # Main dashboard
│   ├── Analytics.cshtml          # Analytics dashboard
│   ├── ManageDonors.cshtml       # Donor management
│   ├── Inventory.cshtml          # Blood inventory
│   └── ... (15+ other admin views)
├── Donor/                        # Donor portal
│   ├── Dashboard.cshtml          # Donor dashboard
│   ├── Profile.cshtml            # Profile management
│   ├── BookAppointment.cshtml    # Appointment booking
│   ├── Leaderboard.cshtml        # Gamification
│   └── ... (10+ other donor views)
├── Home/                         # Public pages
├── Shared/                       # Layout templates
│   ├── _Layout.cshtml            # Master layout
│   ├── _AdminLayout.cshtml       # Admin layout
│   └── _UserLayout.cshtml        # User layout
```

**Key Concept**: Razor views with C# code embedded
```html
@model DonorViewModel
<h1>@Model.Name</h1>
```

---

### 3. **Core/Entities/** - Data Models
**Purpose**: Define entity classes (database tables)

```
Core/Entities/
├── Donor.cs                      # Donor information
├── BloodInventory.cs             # Blood stock management
├── Appointment.cs                # Donation appointments
├── BloodRequest.cs               # Blood transfusion requests
├── BloodCamp.cs                  # Donation camps
├── DonationRecord.cs             # Historical donations
├── Badge.cs                      # Achievement badges
├── Notification.cs               # User notifications
├── AuditLog.cs                   # Activity logging
├── Feedback.cs                   # User feedback
├── Admin.cs                      # Admin users
├── Reward.cs                     # Reward system
├── RedeemedReward.cs             # Reward redemptions
└── ... (more entities)
```

**Key Concept**: Entity = Database Table
```csharp
public class Donor
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string BloodType { get; set; }
    public string Email { get; set; }
    // Properties map to database columns
}
```

---

### 4. **Data/** - Database Context
**Purpose**: Entity Framework Core configuration

```
Data/
├── ApplicationDbContext.cs        # DbContext (database connection)
├── DbInitializer.cs              # Database initialization/seeding
└── PasswordHasher.cs             # Password hashing utility
```

**Key Concept**: ApplicationDbContext defines all tables
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Donor> Donors { get; set; }
    public DbSet<BloodInventory> BloodInventories { get; set; }
    // Each DbSet = One table
}
```

---

### 5. **Core/Interfaces/** - Abstraction Layer
**Purpose**: Define contracts for repositories

```
Core/Interfaces/
├── IRepository.cs                # Generic repository interface
├── IDonorRepository.cs           # Donor-specific repository
├── IBloodInventoryRepository.cs  # Inventory repository
├── IBloodRequestRepository.cs    # Blood request repository
└── IAppointmentRepository.cs     # Appointment repository
```

**Key Concept**: Interfaces define "what" methods exist, implementations define "how"
```csharp
public interface IDonorRepository : IRepository<Donor>
{
    Task<Donor> GetDonorWithAppointmentsAsync(int id);
    Task<List<Donor>> SearchByBloodTypeAsync(string bloodType);
}
```

---

### 6. **Infrastructure/Repositories/** - Data Access Layer
**Purpose**: Implement repository interfaces (CRUD operations)

```
Infrastructure/Repositories/
├── Repository.cs                 # Generic repository implementation
├── DonorRepository.cs            # Donor data access
├── BloodInventoryRepository.cs   # Inventory data access
├── BloodRequestRepository.cs     # Blood request data access
└── AppointmentRepository.cs      # Appointment data access
```

**Key Concept**: Implements data access logic
```csharp
public class DonorRepository : Repository<Donor>, IDonorRepository
{
    public async Task<Donor> GetDonorWithAppointmentsAsync(int id)
    {
        // Database query logic here
    }
}
```

---

### 7. **Application/Services/** - Business Logic Layer
**Purpose**: Implement business logic and operations

```
Application/Services/
├── BloodAnalyticsService.cs      # Analytics calculations
├── DonorRecommendationService.cs # ML-based recommendations
├── GamificationService.cs        # Badge & reward logic
├── GoogleMapsService.cs          # Map integration
├── JwtService.cs                 # JWT token generation
├── QrCodeService.cs              # QR code generation
├── IBloodAnalyticsService.cs     # Interface
├── IDonorRecommendationService.cs# Interface
├── IGamificationService.cs       # Interface
└── ... (more service interfaces)
```

**Key Concept**: Services contain business logic (not just data access)
```csharp
public class GamificationService : IGamificationService
{
    public async Task AwardBadgeAsync(int donorId, string badgeType)
    {
        // Business logic for awarding badges
    }
}
```

---

### 8. **Application/DTOs/** - Data Transfer Objects
**Purpose**: Define data contracts between layers

```
Application/DTOs/
├── DonorDto.cs                   # Donor DTO (what API returns)
├── AppointmentDto.cs             # Appointment DTO
├── BloodInventoryDto.cs          # Inventory DTO
├── BloodRequestDto.cs            # Blood request DTO
├── BadgeDto.cs                   # Badge DTO
├── RewardDto.cs                  # Reward DTO
└── RedeemedRewardDto.cs          # Redeemed reward DTO
```

**Key Concept**: DTOs expose only necessary data
```csharp
public class DonorDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    // Password NOT included in DTO (security)
}
```

---

### 9. **Application/Mappings/** - Auto-Mapping Configuration
**Purpose**: Configure AutoMapper for entity-to-DTO conversion

```
Application/Mappings/
└── MappingProfile.cs             # All mapping configurations
```

**Key Concept**: Automatically converts entities to DTOs
```csharp
CreateMap<Donor, DonorDto>()
    .ForMember(dest => dest.DonationCount, 
        opt => opt.MapFrom(src => src.DonationRecords.Count));
```

---

### 10. **Helpers/** - Utility Classes
**Purpose**: Provide helper functionality

```
Helpers/
├── BloodCompatibilityHelper.cs   # Blood type compatibility logic
├── DonorBadgeHelper.cs           # Badge eligibility logic
├── AuditService.cs               # Audit logging
├── ApiKeyMiddleware.cs           # API key validation
├── DatabaseHealthCheck.cs        # Database health monitoring
└── ExceptionMiddleware.cs        # Global exception handling
```

---

### 11. **Filters/** - Request/Response Filters
**Purpose**: Cross-cutting concerns (authorization, validation)

```
Filters/
└── AuthFilter.cs                 # Authorization filter
```

**Key Concept**: Runs before/after action methods
```csharp
[ServiceFilter(typeof(AuthFilter))]
public IActionResult AdminDashboard() { }
```

---

### 12. **Middleware/** - Request Processing Pipeline
**Purpose**: Handle requests before they reach controllers

```
Middleware/
└── ExceptionMiddleware.cs        # Global exception handling
```

---

### 13. **Migrations/** - Database Version Control
**Purpose**: Track database schema changes

```
Migrations/
├── 20260520220714_ExpandDonorModel.cs
├── 20260520220920_AddDonationRecord.cs
├── 20260521190308_AddCampRegistration.cs
├── ... (12+ migration files)
└── ApplicationDbContextModelSnapshot.cs
```

**Key Concept**: Each migration = one database change
```bash
# Creates migration file
dotnet ef migrations add AddCampRegistration

# Applies migrations to database
dotnet ef database update
```

---

### 14. **Hubs/** - Real-Time Communication
**Purpose**: SignalR hubs for real-time features

```
Hubs/
└── NotificationHub.cs            # Real-time notifications
```

**Key Concept**: WebSocket connection for live updates
```csharp
public class NotificationHub : Hub
{
    public async Task SendNotification(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }
}
```

---

### 15. **Models/** - View Models
**Purpose**: Models used in views

```
Models/
└── ErrorViewModel.cs             # Error page model
```

---

### 16. **Properties/** - Project Settings
**Purpose**: Application configuration

```
Properties/
└── launchSettings.json           # Development/launch settings
```

---

### 17. **wwwroot/** - Static Files
**Purpose**: CSS, JavaScript, images

```
wwwroot/
├── css/
│   ├── site.css                 # Main stylesheet
│   ├── light-theme.css          # Light theme
│   ├── dark-theme.css           # Dark theme
│   └── theme.css                # Theme switching
├── js/
│   └── site.js                  # JavaScript
└── lib/
    └── (External libraries)
```

---

### 18. **Program.cs** - Application Entry Point
**Purpose**: Configure and start application

**Key Responsibilities:**
- Register services (dependency injection)
- Configure middleware pipeline
- Set up database
- Configure authentication
- Start the application

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddScoped<IDonorService, DonorService>();
builder.Services.AddDbContext<ApplicationDbContext>();

var app = builder.Build();

// Configure middleware
app.UseRouting();
app.MapControllers();

app.Run();
```

---

## 🔄 Request Flow

### Example: Creating a Donor

```
1. User submits form in Browser
   └──> HTTP POST /Donor/Register

2. DonorController.Register() receives request
   └──> Validates input
   └──> Calls DonorService.RegisterDonorAsync()

3. DonorService processes business logic
   └──> Hash password
   └──> Check for duplicates
   └──> Calls DonorRepository.AddAsync()

4. DonorRepository performs database operation
   └──> EntityFramework executes INSERT query
   └──> Saves to database

5. Response sent back to User
   └──> Redirect to success page
   └──> or JSON response for API
```

---

## 💾 Data Flow (MVC)

```
User Input (Browser Form)
    ↓
HTTP Request
    ↓
Routing (route matching)
    ↓
Controller Action
    ↓
Service Layer (Business Logic)
    ↓
Repository Layer (Data Access)
    ↓
Entity Framework
    ↓
Database (SQLite/SQL Server)
    ↓
Response (HTML View or JSON)
    ↓
User's Browser
```

---

## 🔐 Security Layers

1. **Authentication** - JWT tokens for API, session cookies for web
2. **Authorization** - Role-based access control (Admin/User)
3. **Password Security** - Hashed with salt
4. **Input Validation** - Both client and server-side
5. **API Key Middleware** - Additional API protection
6. **Audit Logging** - Track all sensitive operations

---

## 🔧 Key Technologies Explained

### ASP.NET Core
- Web framework for building web apps
- Handles HTTP requests/responses
- Built-in dependency injection

### Entity Framework Core
- Object-relational mapper (ORM)
- Maps C# classes to database tables
- Generates SQL automatically

### Razor Views
- HTML templates with C# code
- Generates HTML for browsers
- Server-side rendering

### AutoMapper
- Maps entities to DTOs automatically
- Reduces boilerplate code
- Type-safe mapping

### SignalR
- Real-time communication
- Websocket support
- Push notifications to clients

### Serilog
- Structured logging
- Log to files, console, etc.
- Easy debugging

---

## 📊 Main Features Explained

### 1. **Donor Management**
- Users can register and create profiles
- Stored in `Donors` table
- Contains: Name, Email, BloodType, Phone, Address, etc.

### 2. **Blood Inventory**
- Tracks available blood units
- Organized by blood type (O+, O-, A+, A-, B+, B-, AB+, AB-)
- Updates when donations/requests processed

### 3. **Appointments**
- Donors book donation slots
- Stored with date, time, status
- Admins can confirm/cancel
- Generates DonationRecords when completed

### 4. **Blood Requests**
- Hospitals/patients request blood
- Matched with compatible donors
- Track status (Pending → Fulfilled)
- Real-time notifications

### 5. **Gamification**
- Badges for achievements
- Leaderboards based on donations
- Reward system
- Encourages repeat donations

### 6. **Analytics**
- Dashboard with statistics
- Donation trends
- Blood type distribution
- Active donors count

### 7. **QR Codes**
- Each donor has unique QR code
- Digital identification
- Quick access to donor info

---

## 🎯 Common Development Tasks

### Adding a New Feature
1. **Create Entity** (Core/Entities/)
2. **Create Repository** (Infrastructure/Repositories/)
3. **Create Service** (Application/Services/)
4. **Create DTO** (Application/DTOs/)
5. **Add Migration** (EF Core)
6. **Create Controller** (Controllers/)
7. **Create View** (if MVC) or API endpoint

### Modifying Database
```bash
# 1. Change entity in Core/Entities/
# 2. Create migration
dotnet ef migrations add FeatureName

# 3. Review generated migration file
# 4. Apply to database
dotnet ef database update
```

### Adding API Endpoint
1. Create/modify controller method
2. Add [HttpGet/Post/Put/Delete] attribute
3. Return ActionResult<TDto>
4. Document in Swagger

---

## 📈 Development Workflow

### Daily Workflow:
```
1. Pull latest code
   git pull origin main

2. Create feature branch
   git checkout -b feature/new-feature

3. Make changes
   - Modify files
   - Test locally
   - Create migration if database changed

4. Commit and push
   git add .
   git commit -m "Add: feature description"
   git push origin feature/new-feature

5. Create Pull Request
   - Request code review
   - Fix review comments

6. Merge to main
   - After approval
   - Deploy if needed
```

---

## 🧪 Testing the Application

### Run Application
```bash
cd BloodDonationApp
dotnet restore
dotnet run
```

### Access Application
- **Web App**: http://localhost:5000
- **API Docs**: http://localhost:5000/swagger
- **Admin Panel**: http://localhost:5000/Admin

### Test Users (Pre-seeded)
- Check `Data/DbInitializer.cs` for test accounts

---

## 📚 File Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Controller | `[Name]Controller.cs` | `DonorController.cs` |
| Service | `[Name]Service.cs` | `DonorService.cs` |
| Service Interface | `I[Name]Service.cs` | `IDonorService.cs` |
| Repository | `[Name]Repository.cs` | `DonorRepository.cs` |
| Entity | `[Name].cs` | `Donor.cs` |
| DTO | `[Name]Dto.cs` | `DonorDto.cs` |
| View | `[Name].cshtml` | `Index.cshtml` |
| Migration | Timestamp + Name | `20260520220714_AddDonor.cs` |

---

## 🚀 Next Steps

1. **Understand the codebase**: Read through key files
2. **Set up locally**: Clone, restore, run
3. **Explore features**: Test all user interactions
4. **Push to GitHub**: Follow GITHUB_PUSH_GUIDE.md
5. **Start contributing**: Follow development workflow
6. **Document changes**: Update README as you add features

---

## 📞 Questions?

- **Git/GitHub**: See `GITHUB_PUSH_GUIDE.md`
- **Quick start**: See `QUICK_START.md`
- **ASP.NET Core**: https://learn.microsoft.com/en-us/aspnet/core/
- **Entity Framework**: https://learn.microsoft.com/en-us/ef/core/

