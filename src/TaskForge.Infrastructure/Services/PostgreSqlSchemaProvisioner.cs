using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Infrastructure.Persistence;

namespace TaskForge.Infrastructure.Services;

public class PostgreSqlSchemaProvisioner(
    TaskForgeDbContext context,
    ILogger<PostgreSqlSchemaProvisioner> logger) : ISchemaProvisioner
{
    private static readonly Regex SafeSchemaRegex = new(@"^[a-z0-9_]{3,63}$", RegexOptions.Compiled);

    public async Task ProvisionTenantSchemaAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaName) || !SafeSchemaRegex.IsMatch(schemaName))
        {
            throw new ArgumentException($"Invalid schema name: '{schemaName}'. Must be 3-63 alphanumeric/underscore characters.", nameof(schemaName));
        }

        logger.LogInformation("Provisioning tenant PostgreSQL schema: {SchemaName}", schemaName);

        var ddlSql = $@"
            CREATE SCHEMA IF NOT EXISTS ""{schemaName}"";

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""workspaces"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""Name"" character varying(100) NOT NULL,
                ""Slug"" character varying(100) NOT NULL,
                ""Description"" text,
                ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                ""CreatedBy"" character varying(100),
                ""LastModifiedAtUtc"" timestamp with time zone,
                ""LastModifiedBy"" character varying(100)
            );

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""projects"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""WorkspaceId"" uuid NOT NULL REFERENCES ""{schemaName}"".""workspaces""(""Id"") ON DELETE CASCADE,
                ""Name"" character varying(100) NOT NULL,
                ""Key"" character varying(10) NOT NULL,
                ""Description"" text,
                ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                ""CreatedBy"" character varying(100),
                ""LastModifiedAtUtc"" timestamp with time zone,
                ""LastModifiedBy"" character varying(100)
            );

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""boards"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""ProjectId"" uuid NOT NULL REFERENCES ""{schemaName}"".""projects""(""Id"") ON DELETE CASCADE,
                ""Name"" character varying(100) NOT NULL,
                ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                ""CreatedBy"" character varying(100),
                ""LastModifiedAtUtc"" timestamp with time zone,
                ""LastModifiedBy"" character varying(100)
            );

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""board_columns"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""BoardId"" uuid NOT NULL REFERENCES ""{schemaName}"".""boards""(""Id"") ON DELETE CASCADE,
                ""Name"" character varying(100) NOT NULL,
                ""Order"" integer NOT NULL,
                ""TargetStatus"" integer NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""tasks"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""ColumnId"" uuid NOT NULL REFERENCES ""{schemaName}"".""board_columns""(""Id"") ON DELETE CASCADE,
                ""AssigneeId"" uuid REFERENCES ""public"".""users""(""Id"") ON DELETE SET NULL,
                ""Title"" character varying(200) NOT NULL,
                ""Description"" text,
                ""Priority"" integer NOT NULL,
                ""Status"" integer NOT NULL,
                ""Position"" double precision NOT NULL,
                ""DueDateUtc"" timestamp with time zone,
                ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                ""CreatedBy"" character varying(100),
                ""LastModifiedAtUtc"" timestamp with time zone,
                ""LastModifiedBy"" character varying(100)
            );

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""subtasks"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""TaskItemId"" uuid NOT NULL REFERENCES ""{schemaName}"".""tasks""(""Id"") ON DELETE CASCADE,
                ""Title"" character varying(200) NOT NULL,
                ""IsCompleted"" boolean NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""comments"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""TaskItemId"" uuid NOT NULL REFERENCES ""{schemaName}"".""tasks""(""Id"") ON DELETE CASCADE,
                ""AuthorId"" uuid NOT NULL REFERENCES ""public"".""users""(""Id"") ON DELETE RESTRICT,
                ""Content"" text NOT NULL,
                ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                ""CreatedBy"" character varying(100),
                ""LastModifiedAtUtc"" timestamp with time zone,
                ""LastModifiedBy"" character varying(100)
            );

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""labels"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""WorkspaceId"" uuid NOT NULL REFERENCES ""{schemaName}"".""workspaces""(""Id"") ON DELETE CASCADE,
                ""Name"" character varying(50) NOT NULL,
                ""ColorHex"" character varying(10) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""attachments"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""TaskItemId"" uuid NOT NULL REFERENCES ""{schemaName}"".""tasks""(""Id"") ON DELETE CASCADE,
                ""UploadedById"" uuid NOT NULL REFERENCES ""public"".""users""(""Id"") ON DELETE RESTRICT,
                ""FileName"" character varying(256) NOT NULL,
                ""StoragePath"" character varying(500) NOT NULL,
                ""ContentType"" character varying(100) NOT NULL,
                ""SizeBytes"" bigint NOT NULL,
                ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                ""CreatedBy"" character varying(100),
                ""LastModifiedAtUtc"" timestamp with time zone,
                ""LastModifiedBy"" character varying(100)
            );

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""activity_logs"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""UserId"" uuid NOT NULL REFERENCES ""public"".""users""(""Id"") ON DELETE CASCADE,
                ""Action"" integer NOT NULL,
                ""EntityName"" character varying(100) NOT NULL,
                ""EntityId"" character varying(100) NOT NULL,
                ""Details"" text,
                ""TimestampUtc"" timestamp with time zone NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""notifications"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""UserId"" uuid NOT NULL REFERENCES ""public"".""users""(""Id"") ON DELETE CASCADE,
                ""Title"" character varying(200) NOT NULL,
                ""Message"" text NOT NULL,
                ""IsRead"" boolean NOT NULL,
                ""CreatedAtUtc"" timestamp with time zone NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""webhooks"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""Url"" character varying(500) NOT NULL,
                ""Secret"" character varying(100) NOT NULL,
                ""Events"" character varying(256) NOT NULL,
                ""IsActive"" boolean NOT NULL
            );
        ";

        await context.Database.ExecuteSqlRawAsync(ddlSql, cancellationToken);
        logger.LogInformation("Successfully provisioned tenant schema: {SchemaName}", schemaName);
    }
}
