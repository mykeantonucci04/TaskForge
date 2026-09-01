using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Domain.Entities;
using TaskForge.Infrastructure.Persistence;

namespace TaskForge.Infrastructure.Services;

public interface ITenantProvisioningService : ISchemaProvisioner
{
    Task ProvisionTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task RollbackTenantProvisioningAsync(string schemaName, CancellationToken cancellationToken = default);
}

public partial class TenantProvisioningService : ITenantProvisioningService
{
    private readonly PublicDbContext _publicDbContext;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        PublicDbContext publicDbContext,
        ILogger<TenantProvisioningService> logger)
    {
        _publicDbContext = publicDbContext;
        _logger = logger;
    }

    [GeneratedRegex(@"^tenant_[a-z0-9_]{1,55}$", RegexOptions.Compiled)]
    private static partial Regex SchemaNameRegex();

    public async Task ProvisionTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        await ProvisionTenantSchemaAsync(tenant.SchemaName, cancellationToken);
    }

    public async Task ProvisionTenantSchemaAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        // 1. Validate & Sanitize schema name to prevent SQL Injection
        var sanitizedSchema = ValidateAndSanitizeSchemaName(schemaName);

        _logger.LogInformation("Starting provisioning for tenant schema: {SchemaName}", sanitizedSchema);

        try
        {
            // 2. CREATE SCHEMA
            var createSchemaSql = $"CREATE SCHEMA IF NOT EXISTS \"{sanitizedSchema}\";";
            await _publicDbContext.Database.ExecuteSqlRawAsync(createSchemaSql, cancellationToken);

            // 3. Create Tenant-Scoped Tables for TenantDbContext
            var ddlSql = BuildTenantTablesDdl(sanitizedSchema);
            await _publicDbContext.Database.ExecuteSqlRawAsync(ddlSql, cancellationToken);

            _logger.LogInformation("Successfully provisioned tenant schema: {SchemaName}", sanitizedSchema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision tenant schema: {SchemaName}. Initiating rollback...", sanitizedSchema);
            await RollbackTenantProvisioningAsync(sanitizedSchema, CancellationToken.None);
            throw new InvalidOperationException($"Failed to provision tenant schema '{sanitizedSchema}'. Rollback executed.", ex);
        }
    }

    public async Task RollbackTenantProvisioningAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedSchema = ValidateAndSanitizeSchemaName(schemaName);
            _logger.LogWarning("Executing rollback: DROPPING schema {SchemaName} CASCADE", sanitizedSchema);

            var dropSchemaSql = $"DROP SCHEMA IF EXISTS \"{sanitizedSchema}\" CASCADE;";
            await _publicDbContext.Database.ExecuteSqlRawAsync(dropSchemaSql, cancellationToken);
            _logger.LogInformation("Rollback completed for schema: {SchemaName}", sanitizedSchema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during rollback for schema: {SchemaName}", schemaName);
        }
    }

    public static string ValidateAndSanitizeSchemaName(string schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            throw new ArgumentException("Schema name cannot be null or empty.", nameof(schemaName));
        }

        var normalized = schemaName.Trim().ToLowerInvariant();

        if (!SchemaNameRegex().IsMatch(normalized))
        {
            throw new ArgumentException(
                $"Invalid schema name '{schemaName}'. Schema name must match pattern 'tenant_[a-z0-9_]' and be between 8 and 63 characters.",
                nameof(schemaName));
        }

        return normalized;
    }

    private static string BuildTenantTablesDdl(string schema) => $"""
        CREATE TABLE IF NOT EXISTS "{schema}"."workspaces" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "Name" character varying(100) NOT NULL,
            "Slug" character varying(100) NOT NULL,
            "Description" text,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "CreatedBy" character varying(100),
            "LastModifiedAtUtc" timestamp with time zone,
            "LastModifiedBy" character varying(100)
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "ix_{schema}_workspaces_slug" ON "{schema}"."workspaces" ("Slug");

        CREATE TABLE IF NOT EXISTS "{schema}"."projects" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "WorkspaceId" uuid NOT NULL REFERENCES "{schema}"."workspaces"("Id") ON DELETE CASCADE,
            "Name" character varying(100) NOT NULL,
            "Key" character varying(10) NOT NULL,
            "Description" text,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "CreatedBy" character varying(100),
            "LastModifiedAtUtc" timestamp with time zone,
            "LastModifiedBy" character varying(100)
        );

        CREATE TABLE IF NOT EXISTS "{schema}"."boards" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "ProjectId" uuid NOT NULL REFERENCES "{schema}"."projects"("Id") ON DELETE CASCADE,
            "Name" character varying(100) NOT NULL,
            "Description" text,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "CreatedBy" character varying(100),
            "LastModifiedAtUtc" timestamp with time zone,
            "LastModifiedBy" character varying(100)
        );

        CREATE TABLE IF NOT EXISTS "{schema}"."board_columns" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "BoardId" uuid NOT NULL REFERENCES "{schema}"."boards"("Id") ON DELETE CASCADE,
            "Name" character varying(100) NOT NULL,
            "Position" integer NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "CreatedBy" character varying(100),
            "LastModifiedAtUtc" timestamp with time zone,
            "LastModifiedBy" character varying(100)
        );

        CREATE TABLE IF NOT EXISTS "{schema}"."tasks" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "ColumnId" uuid NOT NULL REFERENCES "{schema}"."board_columns"("Id") ON DELETE CASCADE,
            "AssigneeId" uuid REFERENCES "public"."users"("Id") ON DELETE SET NULL,
            "Title" character varying(200) NOT NULL,
            "Description" text,
            "Priority" integer NOT NULL,
            "Status" integer NOT NULL,
            "Position" double precision NOT NULL,
            "DueDateUtc" timestamp with time zone,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "CreatedBy" character varying(100),
            "LastModifiedAtUtc" timestamp with time zone,
            "LastModifiedBy" character varying(100)
        );

        CREATE TABLE IF NOT EXISTS "{schema}"."subtasks" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "TaskItemId" uuid NOT NULL REFERENCES "{schema}"."tasks"("Id") ON DELETE CASCADE,
            "Title" character varying(200) NOT NULL,
            "IsCompleted" boolean NOT NULL
        );

        CREATE TABLE IF NOT EXISTS "{schema}"."comments" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "TaskItemId" uuid NOT NULL REFERENCES "{schema}"."tasks"("Id") ON DELETE CASCADE,
            "AuthorId" uuid NOT NULL REFERENCES "public"."users"("Id") ON DELETE RESTRICT,
            "Content" text NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "CreatedBy" character varying(100),
            "LastModifiedAtUtc" timestamp with time zone,
            "LastModifiedBy" character varying(100)
        );

        CREATE TABLE IF NOT EXISTS "{schema}"."labels" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "WorkspaceId" uuid NOT NULL REFERENCES "{schema}"."workspaces"("Id") ON DELETE CASCADE,
            "Name" character varying(50) NOT NULL,
            "ColorHex" character varying(10) NOT NULL
        );

        CREATE TABLE IF NOT EXISTS "{schema}"."attachments" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "TaskItemId" uuid NOT NULL REFERENCES "{schema}"."tasks"("Id") ON DELETE CASCADE,
            "UploadedById" uuid NOT NULL REFERENCES "public"."users"("Id") ON DELETE RESTRICT,
            "FileName" character varying(256) NOT NULL,
            "StoragePath" character varying(500) NOT NULL,
            "ContentType" character varying(100) NOT NULL,
            "SizeBytes" bigint NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "CreatedBy" character varying(100),
            "LastModifiedAtUtc" timestamp with time zone,
            "LastModifiedBy" character varying(100)
        );

        CREATE TABLE IF NOT EXISTS "{schema}"."activity_logs" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "UserId" uuid NOT NULL REFERENCES "public"."users"("Id") ON DELETE CASCADE,
            "Action" integer NOT NULL,
            "EntityName" character varying(100) NOT NULL,
            "EntityId" character varying(100) NOT NULL,
            "Details" text,
            "TimestampUtc" timestamp with time zone NOT NULL
        );

        CREATE TABLE IF NOT EXISTS "{schema}"."notifications" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "UserId" uuid NOT NULL REFERENCES "public"."users"("Id") ON DELETE CASCADE,
            "Title" character varying(200) NOT NULL,
            "Message" text NOT NULL,
            "IsRead" boolean NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL
        );

        CREATE TABLE IF NOT EXISTS "{schema}"."webhooks" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "Url" character varying(500) NOT NULL,
            "Secret" character varying(100) NOT NULL,
            "Events" character varying(256) NOT NULL,
            "IsActive" boolean NOT NULL
        );
    """;
}
