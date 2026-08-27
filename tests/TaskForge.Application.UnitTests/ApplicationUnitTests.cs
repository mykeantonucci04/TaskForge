using FluentAssertions;
using NSubstitute;
using TaskForge.Application.Features.Tenants;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;
using Xunit;

namespace TaskForge.Application.UnitTests;

public class ApplicationUnitTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task CreateTenantCommandHandler_Should_Create_Tenant_When_Identifier_Is_Unique()
    {
        // Arrange
        _tenantRepository.GetByIdentifierAsync("acme-inc", Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var handler = new CreateTenantCommandHandler(_tenantRepository, _unitOfWork);
        var command = new CreateTenantCommand("Acme Corp", "acme-inc");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Acme Corp");
        result.Value.Identifier.Should().Be("acme-inc");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTenantCommandHandler_Should_Return_Conflict_When_Identifier_Exists()
    {
        // Arrange
        var existingTenant = new Tenant { Id = Guid.NewGuid(), Identifier = "acme-inc" };
        _tenantRepository.GetByIdentifierAsync("acme-inc", Arg.Any<CancellationToken>())
            .Returns(existingTenant);

        var handler = new CreateTenantCommandHandler(_tenantRepository, _unitOfWork);
        var command = new CreateTenantCommand("Acme Corp", "acme-inc");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.Exists");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
