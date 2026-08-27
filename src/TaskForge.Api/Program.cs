using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TaskForge.Api.Endpoints;
using TaskForge.Api.Middleware;
using TaskForge.Api.Services;
using TaskForge.Application;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Infrastructure;
using TaskForge.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Layer Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ASP.NET Core 10 Metrics for Auth
builder.Services.AddMetrics();

// JWT Bearer Authentication Setup
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"] ?? "SuperSecretKeyForTaskForgeBackend1234567890!";
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
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "TaskForgeApi",
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "TaskForgeClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});

builder.Services.AddAuthorization();

// Health Checks
var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=taskforge_db;Username=taskforge_user;Password=TaskForgePass123!";
builder.Services.AddHealthChecks()
    .AddNpgSql(dbConnectionString, name: "postgresql", tags: new[] { "db", "postgres" });

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
