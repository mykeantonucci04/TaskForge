using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using TaskForge.Api.Endpoints;
using TaskForge.Api.Middleware;
using TaskForge.Api.Services;
using TaskForge.Application;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Infrastructure;
using TaskForge.Infrastructure.Extensions;
using TaskForge.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. DYNAMIC PORT CONFIGURATION (Render)
// ==========================================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ==========================================
// 2. DATABASE CONFIGURATION RESOLUTION
// ==========================================
var (resolvedDbConnectionString, dbConfigSource) = builder.Configuration.ResolveDatabaseConnectionString();

// ==========================================
// 3. JWT CONFIGURATION RESOLUTION
// ==========================================
var jwtSecretKey = builder.Configuration["Jwt:Secret"]
    ?? builder.Configuration["JwtSettings:SecretKey"]
    ?? builder.Configuration["Jwt:SecretKey"]
    ?? "SuperSecretKeyForTaskForgeBackend1234567890!";

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? builder.Configuration["JwtSettings:Issuer"]
    ?? "TaskForgeApi";

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? builder.Configuration["JwtSettings:Audience"]
    ?? "TaskForgeClient";

var jwtSource = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Secret"])
    ? "Jwt:Secret (Environment / appsettings)"
    : (!string.IsNullOrWhiteSpace(builder.Configuration["JwtSettings:SecretKey"]) ? "JwtSettings:SecretKey" : "Development Default Fallback");

// ==========================================
// 4. PRODUCTION STARTUP VALIDATION
// ==========================================
if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(resolvedDbConnectionString))
    {
        throw new InvalidOperationException(
            "FATAL STARTUP ERROR: Database connection string is not configured. " +
            "Please set the 'DATABASE_URL' (from Neon) or 'ConnectionStrings__DefaultConnection' environment variable in your Render dashboard.");
    }

    if (string.IsNullOrWhiteSpace(jwtSecretKey) || jwtSecretKey.Contains("SuperSecretKey", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "FATAL STARTUP ERROR: JWT Secret is not configured or is using the development fallback key in Production. " +
            "Please set the 'Jwt__Secret' environment variable in your Render dashboard.");
    }
}

// ==========================================
// 5. STARTUP OBSERVABILITY LOGGING
// ==========================================
using var startupLoggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});
var startupLogger = startupLoggerFactory.CreateLogger("TaskForge.Startup");
startupLogger.LogInformation("TaskForge API initializing on port {Port} in [{Environment}] mode", port, builder.Environment.EnvironmentName);
startupLogger.LogInformation("Database connection resolved from: {DbSource}", dbConfigSource);
startupLogger.LogInformation("JWT authentication configured from: {JwtSource} (Issuer: {Issuer}, Audience: {Audience})", jwtSource, jwtIssuer, jwtAudience);

// ==========================================
// 6. SERVICE REGISTRATIONS
// ==========================================
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, resolvedDbConnectionString);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Metrics for Auth
builder.Services.AddMetrics();

// JWT Bearer Authentication Setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});

builder.Services.AddAuthorization();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        !string.IsNullOrWhiteSpace(resolvedDbConnectionString)
            ? resolvedDbConnectionString
            : "Host=localhost;Port=5432;Database=taskforge_db;Username=taskforge_user;Password=TaskForgePass123!",
        name: "postgresql",
        tags: ["db", "postgres"]);

// OpenAPI 3.1 Setup
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "TaskForge Multi-Tenant Enterprise API",
            Version = "v1",
            Description = "Clean Architecture Multi-Tenant Backend with Schema-Per-Tenant Isolation"
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("TaskForge API Documentation")
            .WithTheme(ScalarTheme.Moon)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

// Map Endpoints & Hubs
app.MapApiEndpoints();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
