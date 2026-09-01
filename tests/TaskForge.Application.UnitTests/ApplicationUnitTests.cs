using FluentAssertions;
using NSubstitute;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Features.Tenants;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Interfaces;
using Xunit;

namespace TaskForge.Application.UnitTests;

public class ApplicationUnitTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITenantMembershipRepository _membershipRepository = Substitute.For<ITenantMembershipRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ISchemaProvisioner _schemaProvisioner = Substitute.For<ISchemaProvisioner>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();

    // ==========================================
    // RegisterTenantCommandHandler Tests
    // ==========================================

    [Fact]
    public async Task RegisterTenantCommandHandler_Should_Provision_Tenant_Successfully()
    {
        // Arrange
        var command = new RegisterTenantCommand(
            "Acme Innovations",
            "acme-innovations",
            "John",
            "Doe",
            "john.doe@acme.com",
            "SecurePass123!",
            TenantPlan.Pro);

        _tenantRepository.ExistsBySlugAsync(Arg.Any<TenantSlug>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _passwordHasher.HashPassword("SecurePass123!")
            .Returns("hashed_secure_pass");

        _tokenGenerator.GenerateTokens(Arg.Any<Guid>(), "john.doe@acme.com", "Owner", Arg.Any<Guid>(), "tenant_acme_innovations")
            .Returns(("access_token_jwt", "refresh_token_xyz", DateTime.UtcNow.AddDays(7)));

        var handler = new RegisterTenantCommandHandler(
            _tenantRepository,
            _userRepository,
            _membershipRepository,
            _unitOfWork,
            _passwordHasher,
            _schemaProvisioner,
            _tokenGenerator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyName.Should().Be("Acme Innovations");
        result.Value.Slug.Should().Be("acme-innovations");
        result.Value.SchemaName.Should().Be("tenant_acme_innovations");
        result.Value.Plan.Should().Be(TenantPlan.Pro);
        result.Value.Role.Should().Be("Owner");
        result.Value.AdminEmail.Should().Be("john.doe@acme.com");
        result.Value.AdminFullName.Should().Be("John Doe");
        result.Value.AccessToken.Should().Be("access_token_jwt");
        result.Value.RefreshToken.Should().Be("refresh_token_xyz");

        await _tenantRepository.Received(1).AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _membershipRepository.Received(1).AddAsync(Arg.Any<TenantMembership>(), Arg.Any<CancellationToken>());
        await _schemaProvisioner.Received(1).ProvisionTenantSchemaAsync("tenant_acme_innovations", Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterTenantCommandHandler_Should_Return_Conflict_When_Slug_Exists()
    {
        // Arrange
        var command = new RegisterTenantCommand(
            "Acme Innovations",
            "acme-innovations",
            "John",
            "Doe",
            "john.doe@acme.com",
            "SecurePass123!");

        _tenantRepository.ExistsBySlugAsync(Arg.Any<TenantSlug>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new RegisterTenantCommandHandler(
            _tenantRepository,
            _userRepository,
            _membershipRepository,
            _unitOfWork,
            _passwordHasher,
            _schemaProvisioner,
            _tokenGenerator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.SlugExists");
        await _tenantRepository.DidNotReceive().AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
        await _schemaProvisioner.DidNotReceive().ProvisionTenantSchemaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ==========================================
    // CreateTenantCommandHandler Tests
    // ==========================================

    [Fact]
    public async Task CreateTenantCommandHandler_Should_Create_Tenant_When_Slug_Is_Unique()
    {
        // Arrange
        _tenantRepository.ExistsBySlugAsync(Arg.Any<TenantSlug>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CreateTenantCommandHandler(_tenantRepository, _unitOfWork);
        var command = new CreateTenantCommand("Acme Corp", "acme-inc");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Acme Corp");
        result.Value.Slug.Should().Be("acme-inc");
        result.Value.SchemaName.Should().Be("tenant_acme_inc");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTenantCommandHandler_Should_Return_Conflict_When_Slug_Exists()
    {
        // Arrange
        _tenantRepository.ExistsBySlugAsync(Arg.Any<TenantSlug>(), Arg.Any<CancellationToken>())
            .Returns(true);

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
