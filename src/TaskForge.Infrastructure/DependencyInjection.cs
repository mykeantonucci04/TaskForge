using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Domain.Interfaces;
using TaskForge.Infrastructure.Extensions;
using TaskForge.Infrastructure.Persistence;
using TaskForge.Infrastructure.Persistence.Interceptors;
using TaskForge.Infrastructure.Persistence.Repositories;
using TaskForge.Infrastructure.Services;

namespace TaskForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? databaseConnectionString = null)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<TenantEntityInterceptor>();

        var connectionString = !string.IsNullOrWhiteSpace(databaseConnectionString)
            ? databaseConnectionString
            : configuration.ResolveDatabaseConnectionString().ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Database=taskforge_db;Username=postgres;Password=postgres";
        }

        // 1. PublicDbContext for public schema tables
        services.AddDbContext<PublicDbContext>((sp, options) =>
        {
            var auditableInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PublicDbContext).Assembly.FullName);
            })
            .AddInterceptors(auditableInterceptor);
        });

        // 2. TenantDbContext for dynamic schema-per-tenant tables
        services.AddDbContext<TenantDbContext>((sp, options) =>
        {
            var auditableInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            var tenantInterceptor = sp.GetRequiredService<TenantEntityInterceptor>();

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName);
            })
            .AddInterceptors(auditableInterceptor, tenantInterceptor);
        });

        // Repositories & UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(PublicRepository<>));
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantMembershipRepository, TenantMembershipRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IBoardColumnRepository, BoardColumnRepository>();

        // Infrastructure Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<ISchemaProvisioner>(sp => sp.GetRequiredService<ITenantProvisioningService>());
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenService>();
        services.AddScoped<IStorageService, S3StorageService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<INotificationService, SignalRNotificationService>();
        services.AddSignalR();

        return services;
    }
}
