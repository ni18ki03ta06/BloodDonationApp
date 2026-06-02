using BloodDonationApp.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Infrastructure.Repositories;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Application.Services;
using BloodDonationApp.Application.Mappings;

// Determine minimum level based on environment to bootstrap logger early if needed
var isDevEnv = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);
var bootstrapMinLogLevel = isDevEnv ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(bootstrapMinLogLevel)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting BloodDonationApp host...");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog as host logging provider
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        if (builder.Environment.IsProduction())
        {
            options.UseSqlServer(connStr);
        }
        else
        {
            options.UseSqlite(connStr);
        }
    });

    builder.Services.AddHealthChecks()
        .AddCheck<BloodDonationApp.Helpers.DatabaseHealthCheck>("database");

    // Enable Session State support and Memory Cache
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30); // Session duration
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    });
    builder.Services.AddHttpContextAccessor(); // Allows accessing HttpContext in helper classes/filters
    builder.Services.AddScoped<BloodDonationApp.Services.IGoogleMapsService, BloodDonationApp.Services.GoogleMapsService>();
    builder.Services.AddScoped<BloodDonationApp.Services.IDonorRecommendationService, BloodDonationApp.Services.DonorRecommendationService>();
    builder.Services.AddScoped<BloodDonationApp.Services.IJwtService, BloodDonationApp.Services.JwtService>();
    builder.Services.AddScoped<BloodDonationApp.Services.IBloodAnalyticsService, BloodDonationApp.Services.BloodAnalyticsService>();
    builder.Services.AddScoped<BloodDonationApp.Services.IGamificationService, BloodDonationApp.Services.GamificationService>();
    builder.Services.AddSingleton<BloodDonationApp.Services.IQrCodeService, BloodDonationApp.Services.QrCodeService>();

    // Clean Architecture Repositories
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IDonorRepository, DonorRepository>();
    builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
    builder.Services.AddScoped<IBloodRequestRepository, BloodRequestRepository>();
    builder.Services.AddScoped<IBloodInventoryRepository, BloodInventoryRepository>();

    // Clean Architecture Services
    builder.Services.AddScoped<IDonorService, DonorService>();
    builder.Services.AddScoped<IAppointmentService, AppointmentService>();
    builder.Services.AddScoped<IBloodRequestService, BloodRequestService>();
    builder.Services.AddScoped<IBloodInventoryService, BloodInventoryService>();

    // AutoMapper Setup
    var mapperConfig = new AutoMapper.MapperConfiguration(mc =>
    {
        mc.AddProfile(new MappingProfile());
    }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
    AutoMapper.IMapper mapper = mapperConfig.CreateMapper();
    builder.Services.AddSingleton(mapper);

    builder.Services.AddSignalR();

    // JWT Authentication Setup
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "DonoraBloodDonationAppSuperSecretJWTKey123!";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DonoraIssuer";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DonoraAudience";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

    // API Versioning Setup
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Swagger Setup with JWT Support
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "BloodDonationApp API", Version = "v1" });
        options.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "BloodDonationApp API", Version = "v2" });

        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\""
        });
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // Initialize the database
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            DbInitializer.Initialize(context, app.Environment.IsDevelopment());
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred creating the DB.");
        }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    // Configure custom ExceptionMiddleware for API routes
    app.UseMiddleware<BloodDonationApp.Middleware.ExceptionMiddleware>();

    // Capture status codes (such as 404) and re-execute through the Error handler
    app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

    // Security Response Headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        await next();
    });

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BloodDonationApp API v1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "BloodDonationApp API v2");
    });

    app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var isDbHealthy = report.Entries.TryGetValue("database", out var entry) && 
                              entry.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy;
            var response = new
            {
                status = report.Status.ToString(),
                database = isDbHealthy ? "Connected" : "Disconnected"
            };
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    });

    app.UseMiddleware<BloodDonationApp.Helpers.ApiKeyMiddleware>();

    app.UseRouting();

    app.UseSession(); // Enable session middleware

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHub<BloodDonationApp.Hubs.NotificationHub>("/notificationHub");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
