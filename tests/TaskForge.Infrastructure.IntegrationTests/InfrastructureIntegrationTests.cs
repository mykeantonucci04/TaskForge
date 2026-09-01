using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Infrastructure.Extensions;
using TaskForge.Infrastructure.Persistence;
using TaskForge.Infrastructure.Services;
using TaskForge.Shared.Constants;
using Testcontainers.PostgreSql;
using Xunit;

namespace TaskForge.Infrastructure.IntegrationTests;

public class InfrastructureIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("taskforge_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task PublicDbContext_Should_Create_And_Query_Tenants_In_Public_Schema()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PublicDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        await using var context = new PublicDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var slug = TenantSlug.Create("test-tenant");
        var tenant = Tenant.Create("Integration Test Tenant", slug, TenantPlan.Enterprise);

        // Act
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var fetched = await context.Tenants.FirstOrDefaultAsync(t => t.Slug == slug);

        // Assert
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Integration Test Tenant");
        fetched.Slug.Value.Should().Be("test-tenant");
        fetched.Plan.Should().Be(TenantPlan.Enterprise);
        fetched.SchemaName.Should().Be("tenant_test_tenant");
    }

    [Fact]
    public async Task TenantProvisioningService_Should_Provision_Schema_And_Tables_Successfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PublicDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        await using var context = new PublicDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var service = new TenantProvisioningService(context, NullLogger<TenantProvisioningService>.Instance);
        var schemaName = "tenant_test_company";

        // Act
        await service.ProvisionTenantSchemaAsync(schemaName);

        // Assert: Verify schema exists in PostgreSQL
        var schemaExists = false;
        await using (var cmd = context.Database.GetDbConnection().CreateCommand())
        {
            await context.Database.OpenConnectionAsync();
            cmd.CommandText = $"SELECT EXISTS(SELECT 1 FROM information_schema.schemata WHERE schema_name = '{schemaName}');";
            var result = await cmd.ExecuteScalarAsync();
            schemaExists = result is bool b && b;
        }

        schemaExists.Should().BeTrue();
    }

    [Theory]
    [InlineData("tenant_invalid; DROP TABLE users;--")]
    [InlineData("invalid_prefix")]
    [InlineData("tenant_with spaces")]
    public void TenantProvisioningService_Should_Throw_On_Malicious_Or_Invalid_Schema_Name(string maliciousSchema)
    {
        var action = () => TenantProvisioningService.ValidateAndSanitizeSchemaName(maliciousSchema);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PasswordHasher_Should_Hash_And_Verify_Password_Using_BCrypt()
    {
        var hasher = new PasswordHasher();
        var rawPassword = "StrongPassword!2026";

        var hash = hasher.HashPassword(rawPassword);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().StartWith("$2"); // BCrypt signature

        hasher.VerifyPassword(rawPassword, hash).Should().BeTrue();
        hasher.VerifyPassword("WrongPassword", hash).Should().BeFalse();
    }

    [Fact]
    public void JwtTokenService_Should_Generate_Tokens_With_Required_Claims()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "VeryStrongSecretKeyForJwtAuthenticationTesting1234567890!",
                ["Jwt:Issuer"] = "TaskForgeTestApi",
                ["Jwt:Audience"] = "TaskForgeTestClient",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

        var service = new JwtTokenService(config);
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var (accessToken, refreshToken, expiry) = service.GenerateTokens(
            userId,
            "admin@acme.com",
            "Owner",
            tenantId,
            "tenant_acme");

        accessToken.Should().NotBeNullOrWhiteSpace();
        refreshToken.Should().NotBeNullOrWhiteSpace();
        expiry.Should().BeAfter(DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        jwt.Issuer.Should().Be("TaskForgeTestApi");
        jwt.Audiences.Should().Contain("TaskForgeTestClient");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "admin@acme.com");
        jwt.Claims.Should().Contain(c => c.Type == TenantConstants.ClaimName && c.Value == tenantId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == TenantConstants.SchemaClaimName && c.Value == "tenant_acme");
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Owner");
    }

    [Theory]
    [InlineData(
        "postgresql://neondb_owner:npg_password123@ep-sample-123.eu-central-1.aws.neon.tech/taskforge_db?sslmode=require",
        "ep-sample-123.eu-central-1.aws.neon.tech",
        "taskforge_db",
        "neondb_owner",
        "npg_password123")]
    [InlineData(
        "postgres://myuser:secret@custom-host.com:5433/mydb",
        "custom-host.com",
        "mydb",
        "myuser",
        "secret")]
    public void ConvertPostgresUriToNpgsqlConnectionString_Should_Parse_Correctly(
        string uri,
        string expectedHost,
        string expectedDb,
        string expectedUsername,
        string expectedPassword)
    {
        var npgsqlConnString = DatabaseConfigurationExtensions.ConvertPostgresUriToNpgsqlConnectionString(uri);

        npgsqlConnString.Should().Contain($"Host={expectedHost}");
        npgsqlConnString.Should().Contain($"Database={expectedDb}");
        npgsqlConnString.Should().Contain($"Username={expectedUsername}");
        npgsqlConnString.Should().Contain($"Password={expectedPassword}");
        npgsqlConnString.Should().ContainEquivalentOf("SSL Mode=Require");
        npgsqlConnString.Should().ContainEquivalentOf("Trust Server Certificate=true");
    }
}
