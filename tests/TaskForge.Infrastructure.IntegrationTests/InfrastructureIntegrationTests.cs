using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Infrastructure.Persistence;
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
    public async Task DbContext_Should_Create_And_Query_Tenants_In_PostgreSQL()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TaskForgeDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        var tenantContext = new TenantContext();
        await using var context = new TaskForgeDbContext(options, tenantContext);
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
}
