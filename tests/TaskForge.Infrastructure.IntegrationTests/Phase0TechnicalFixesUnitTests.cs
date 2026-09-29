using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TaskForge.Infrastructure.Persistence;
using TaskForge.Infrastructure.Services;
using Xunit;

namespace TaskForge.Infrastructure.IntegrationTests;

public class Phase0TechnicalFixesUnitTests
{
    [Fact]
    public void TenantModelCacheKeyFactory_Should_Produce_Distinct_Keys_For_Different_Schemas()
    {
        // Arrange
        var dependencies = new ModelCacheKeyFactoryDependencies();
        var factory = new TenantModelCacheKeyFactory(dependencies);

        var tenantContext1 = new TenantContext();
        tenantContext1.SetTenant(Guid.NewGuid(), "tenant_alpha");
        var options1 = new DbContextOptionsBuilder<TenantDbContext>().Options;
        var dbContext1 = new TenantDbContext(options1, tenantContext1);

        var tenantContext2 = new TenantContext();
        tenantContext2.SetTenant(Guid.NewGuid(), "tenant_beta");
        var options2 = new DbContextOptionsBuilder<TenantDbContext>().Options;
        var dbContext2 = new TenantDbContext(options2, tenantContext2);

        // Act
        var key1 = factory.Create(dbContext1, false);
        var key2 = factory.Create(dbContext2, false);

        // Assert
        key1.Should().NotBeNull();
        key2.Should().NotBeNull();
        key1.Should().NotBe(key2, "each tenant schema must compile and cache its own EF Core model to avoid cross-tenant pollution");
    }

    [Fact]
    public void TenantModelCacheKeyFactory_Should_Produce_Identical_Keys_For_Same_Schema()
    {
        // Arrange
        var dependencies = new ModelCacheKeyFactoryDependencies();
        var factory = new TenantModelCacheKeyFactory(dependencies);

        var tenantContext1 = new TenantContext();
        tenantContext1.SetTenant(Guid.NewGuid(), "tenant_same");
        var options1 = new DbContextOptionsBuilder<TenantDbContext>().Options;
        var dbContext1 = new TenantDbContext(options1, tenantContext1);

        var tenantContext2 = new TenantContext();
        tenantContext2.SetTenant(Guid.NewGuid(), "tenant_same");
        var options2 = new DbContextOptionsBuilder<TenantDbContext>().Options;
        var dbContext2 = new TenantDbContext(options2, tenantContext2);

        // Act
        var key1 = factory.Create(dbContext1, false);
        var key2 = factory.Create(dbContext2, false);

        // Assert
        key1.Should().Be(key2, "contexts with the same tenant schema must reuse the cached EF Core model");
    }

    [Fact]
    public void TenantDbContext_Should_Expose_SchemaName_From_TenantContext()
    {
        // Arrange
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(Guid.NewGuid(), "tenant_production_42");
        var options = new DbContextOptionsBuilder<TenantDbContext>().Options;

        // Act
        var dbContext = new TenantDbContext(options, tenantContext);

        // Assert
        dbContext.SchemaName.Should().Be("tenant_production_42");
    }

    [Fact]
    public void TenantProvisioningService_BuildTenantTablesDdl_Should_Define_Order_And_TargetStatus_On_BoardColumns()
    {
        // Arrange
        var schema = "tenant_sample";

        // Act
        var ddl = TenantProvisioningService.BuildTenantTablesDdl(schema);

        // Assert
        ddl.Should().Contain($"""CREATE TABLE IF NOT EXISTS "{schema}"."board_columns" """);
        ddl.Should().Contain("\"Order\" integer NOT NULL");
        ddl.Should().Contain("\"TargetStatus\" integer NOT NULL DEFAULT 0");
        ddl.Should().NotContain("\"Position\" integer NOT NULL");
    }

    [Fact]
    public void TenantProvisioningService_BuildTenantTablesDdl_Should_Contain_TaskLabels_JoinTable()
    {
        // Arrange
        var schema = "tenant_sample";

        // Act
        var ddl = TenantProvisioningService.BuildTenantTablesDdl(schema);

        // Assert
        ddl.Should().Contain($"""CREATE TABLE IF NOT EXISTS "{schema}"."task_labels" """);
        ddl.Should().Contain("\"TaskId\" uuid NOT NULL");
        ddl.Should().Contain("\"LabelId\" uuid NOT NULL");
        ddl.Should().Contain("PRIMARY KEY (\"TaskId\", \"LabelId\")");
    }

    [Fact]
    public void TenantProvisioningService_BuildTenantTablesDdl_Should_Contain_Safe_Migration_Block_For_Existing_Schemas()
    {
        // Arrange
        var schema = "tenant_sample";

        // Act
        var ddl = TenantProvisioningService.BuildTenantTablesDdl(schema);

        // Assert
        ddl.Should().Contain("RENAME COLUMN \"Position\" TO \"Order\"");
        ddl.Should().Contain("ADD COLUMN \"TargetStatus\" integer NOT NULL DEFAULT 0");
    }
}
