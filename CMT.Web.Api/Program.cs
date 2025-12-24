using CMT.Domain.Data;
using CMT.Application.Interfaces;
using CMT.Application.Services;
using CMT.Web.Api.Data;
using CMT.Web.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Reflection;
using CMT.Web.Api.Hubs;
using SystemTask = System.Threading.Tasks.Task;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Entity Framework with SQL Server (changed from SQLite)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => // Configure SQL Server specific options
        {
            // FIX: INCREASED RETRY SETTINGS to handle persistent Error 10060 (network timeout)
            // Increased retry count to 10 and max delay to 60 seconds to overcome high latency
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 10, // Increased from 5 to 10 attempts
                maxRetryDelay: TimeSpan.FromSeconds(60), // Increased from 30 seconds
                errorNumbersToAdd: null
            );
            
            // Set a Command Timeout to prevent individual queries from hanging indefinitely
            sqlServerOptions.CommandTimeout(60); // 60 seconds timeout
        }
    );
    // Remove warning configuration that's causing issues
});

// Add MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CMT.Application.Services.TaskService).Assembly);
});

// Add SignalR
builder.Services.AddSignalR();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "DefaultSecretKeyForCMTApplication123456789";
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "CMT.Api",
        ValidAudience = jwtSettings["Audience"] ?? "CMT.Client",
        ClockSkew = TimeSpan.Zero
    };
    
    // Configure SignalR JWT authentication
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
            {
                context.Token = accessToken;
            }
            return SystemTask.CompletedTask;
        }
    };
});

// Add Authorization
builder.Services.AddAuthorization();

// Add Application Services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICompletedTaskService, CompletedTaskService>();
builder.Services.AddScoped<DatabaseSeeder>();

// Add notification services - Use SignalR implementation in Web.Api
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<TokenHandlerService>();
builder.Services.AddScoped<INotificationHubService, SignalRNotificationHubService>();
builder.Services.AddScoped<IAmendmentService, AmendmentService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CMT Task Management API",
        Version = "v1",
        Description = "API for CMT Task Management System with Role-Based Access Control"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CMT Task Management API V1");
        c.RoutePrefix = string.Empty;
    });
}

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var seeder = services.GetRequiredService<DatabaseSeeder>();
        
        Console.WriteLine("Initializing database...");
        await context.Database.EnsureCreatedAsync();
        
        Console.WriteLine("Seeding database with initial data...");
        await seeder.SeedAsync();
        
        Console.WriteLine("Database initialization completed successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR Hub
app.MapHub<NotificationHub>("/notificationHub");

// Health check endpoint
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow });

// API info endpoint
app.MapGet("/api/info", () => new 
{ 
    Name = "CMT Task Management API",
    Version = "1.0.0",
    Description = "Role-Based Access Control API for Task Management",
    Timestamp = DateTime.UtcNow
});

// Database connection test endpoint
app.MapGet("/api/db-test", async (ApplicationDbContext context) =>
{
    try
    {
        await context.Database.CanConnectAsync();
        return Results.Ok(new { Status = "Connected", Message = "Database connection successful" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database connection failed: {ex.Message}");
    }
});

// Engineer workload endpoint (for frontend compatibility)
app.MapGet("/api/engineer-workload", () => new
{
    engineers = new[]
    {
        new { 
            engineerName = "John Smith", 
            totalTasks = 15, 
            completedTasks = 12, 
            pendingTasks = 3, 
            workloadPercentage = 80 
        },
        new { 
            engineerName = "Sarah Johnson", 
            totalTasks = 18, 
            completedTasks = 15, 
            pendingTasks = 3, 
            workloadPercentage = 83 
        },
        new { 
            engineerName = "Mike Davis", 
            totalTasks = 10, 
            completedTasks = 8, 
            pendingTasks = 2, 
            workloadPercentage = 80 
        },
        new { 
            engineerName = "Emily Wilson", 
            totalTasks = 22, 
            completedTasks = 18, 
            pendingTasks = 4, 
            workloadPercentage = 82 
        }
    },
    summary = new
    {
        totalEngineers = 4,
        averageWorkload = 81.25,
        totalTasks = 65,
        totalCompleted = 53,
        totalPending = 12
    }
});

app.Run();