using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TaskForge.Application.Features.Tenants;
using TaskForge.Domain.Enums;
using TaskForge.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace TaskForge.Api.IntegrationTests;

public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("taskforge_api_test")
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
    public async Task HealthCheck_Should_Return_Healthy_Status()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegisterTenant_Should_Create_Tenant_And_Return_Created()
    {
        // Arrange
        using var factory = CreateFactory();
        await EnsureDatabaseCreatedAsync(factory);
        var client = factory.CreateClient();

        var payload = new RegisterTenantCommand(
            "Acme Enterprises",
            "acme-ent",
            "Jane",
            "Smith",
            "jane.smith@acme.com",
            "StrongPassword123!",
            TenantPlan.Pro);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/register-tenant", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<RegisterTenantResponseDto>();
        content.Should().NotBeNull();
        content!.CompanyName.Should().Be("Acme Enterprises");
        content.Slug.Should().Be("acme-ent");
        content.SchemaName.Should().Be("tenant_acme_ent");
        content.Role.Should().Be("Owner");
        content.AdminEmail.Should().Be("jane.smith@acme.com");
        content.AccessToken.Should().NotBeNullOrWhiteSpace();
        content.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegisterTenant_Should_Return_Conflict_When_Slug_Already_Exists()
    {
        // Arrange
        using var factory = CreateFactory();
        await EnsureDatabaseCreatedAsync(factory);
        var client = factory.CreateClient();

        var payload1 = new RegisterTenantCommand(
            "Beta Corp",
            "beta-corp",
            "Bob",
            "Taylor",
            "bob@beta.com",
            "StrongPassword123!");

        var payload2 = new RegisterTenantCommand(
            "Beta Second",
            "beta-corp",
            "Alice",
            "Taylor",
            "alice@beta.com",
            "StrongPassword123!");

        // Act
        var response1 = await client.PostAsJsonAsync("/api/v1/auth/register-tenant", payload1);
        var response2 = await client.PostAsJsonAsync("/api/v1/auth/register-tenant", payload2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", _postgresContainer.GetConnectionString());
            });
    }

    private static async Task EnsureDatabaseCreatedAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TaskForgeDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
