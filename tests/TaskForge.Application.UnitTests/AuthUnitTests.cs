using FluentAssertions;
using NSubstitute;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Features.Auth;
using TaskForge.Application.Features.Tenants;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Results;
using Xunit;

namespace TaskForge.Application.UnitTests;

public class AuthUnitTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITenantMembershipRepository _membershipRepository = Substitute.For<ITenantMembershipRepository>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISchemaProvisioner _schemaProvisioner = Substitute.For<ISchemaProvisioner>();

    private LoginCommandHandler CreateLoginHandler()
    {
        return new LoginCommandHandler(
            _userRepository,
            _membershipRepository,
            _tenantRepository,
            _passwordHasher,
            _tokenGenerator,
            _unitOfWork);
    }

    [Fact]
    public async Task Login_With_NonExistent_User_Should_Return_401_Unauthorized_With_Description()
    {
        // Arrange
        var handler = CreateLoginHandler();
        var command = new LoginCommand("nonexistent@example.com", "Password123!");

        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Error.Description.Should().Be("Credenziali non valide o email non registrata.");
    }

    [Fact]
    public async Task Login_With_Invalid_Password_Should_Return_401_Unauthorized()
    {
        // Arrange
        var handler = CreateLoginHandler();
        var command = new LoginCommand("user@example.com", "WrongPassword123!");
        var email = Email.Create("user@example.com");
        var user = User.Create(email, "hashed_password", "Test", "User");

        _userRepository.GetByEmailAsync(Arg.Is<Email>(e => e.Value == "user@example.com"), Arg.Any<CancellationToken>())
            .Returns(user);

        _passwordHasher.VerifyPassword("WrongPassword123!", "hashed_password")
            .Returns(false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Error.Description.Should().Be("Credenziali non valide o email non registrata.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missingatdomain.com")]
    [InlineData("@nodomain.com")]
    [InlineData("plainaddress")]
    [InlineData("")]
    public async Task Login_With_Malformed_Email_Should_Return_401_Without_Throwing_DomainException(string malformedEmail)
    {
        // Arrange
        var handler = CreateLoginHandler();
        var command = new LoginCommand(malformedEmail, "Password123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Error.Description.Should().Be("Credenziali non valide o email non registrata.");
        await _userRepository.DidNotReceive().GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Email_TryCreate_Should_Return_False_For_Invalid_Emails()
    {
        Email.TryCreate("invalid", out var email1).Should().BeFalse();
        email1.Should().BeNull();

        Email.TryCreate(null, out var email2).Should().BeFalse();
        email2.Should().BeNull();

        Email.TryCreate("", out var email3).Should().BeFalse();
        email3.Should().BeNull();

        Email.TryCreate("valid.email@example.com", out var email4).Should().BeTrue();
        email4.Should().NotBeNull();
        email4!.Value.Should().Be("valid.email@example.com");
    }

    [Fact]
    public void RegisterTenantCommandValidator_Should_Pass_When_Admin_Names_Are_Omitted_Or_Defaulted()
    {
        // Arrange
        var validator = new RegisterTenantCommandValidator();
        var command = new RegisterTenantCommand(
            CompanyName: "Acme Innovations",
            Slug: "acme-innovations",
            AdminEmail: "admin@acme.com",
            AdminPassword: "SecurePassword123!");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        command.AdminFirstName.Should().Be("Admin");
        command.AdminLastName.Should().Be("Admin");
    }

    [Fact]
    public void RegisterTenantCommandValidator_Should_Pass_When_Admin_Names_Are_Empty_Strings()
    {
        // Arrange
        var validator = new RegisterTenantCommandValidator();
        var command = new RegisterTenantCommand(
            CompanyName: "Acme Innovations",
            Slug: "acme-innovations",
            AdminFirstName: "",
            AdminLastName: "",
            AdminEmail: "admin@acme.com",
            AdminPassword: "SecurePassword123!");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("password123!", "uppercase")]
    [InlineData("PASSWORD123!", "lowercase")]
    [InlineData("Password!!!!", "digit")]
    [InlineData("Password123", "special character")]
    [InlineData("Pass1!", "at least 8 characters")]
    public void RegisterTenantCommandValidator_Should_Reject_Passwords_Violating_Complexity(string password, string expectedReason)
    {
        // Arrange
        var validator = new RegisterTenantCommandValidator();
        var command = new RegisterTenantCommand(
            CompanyName: "Acme Innovations",
            Slug: "acme-innovations",
            AdminEmail: "admin@acme.com",
            AdminPassword: password);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterTenantCommand.AdminPassword) &&
                                           e.ErrorMessage.Contains(expectedReason, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("StrongPassword123!")]
    [InlineData("Compl3x@Auth#2026")]
    [InlineData("V@lid$Passw0rd-TaskForge")]
    [InlineData("Test_12345/")]
    public void RegisterTenantCommandValidator_Should_Accept_Valid_Complex_Passwords(string validPassword)
    {
        // Arrange
        var validator = new RegisterTenantCommandValidator();
        var command = new RegisterTenantCommand(
            CompanyName: "Acme Innovations",
            Slug: "acme-innovations",
            AdminEmail: "admin@acme.com",
            AdminPassword: validPassword);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterTenantCommandHandler_Should_Fallback_AdminNames_To_Admin_When_Whitespace_Or_Null()
    {
        // Arrange
        var command = new RegisterTenantCommand(
            CompanyName: "Acme Innovations",
            Slug: "acme-innovations",
            AdminFirstName: "   ",
            AdminLastName: "",
            AdminEmail: "admin@acme.com",
            AdminPassword: "SecurePass123!",
            Plan: TenantPlan.Pro);

        _tenantRepository.ExistsBySlugAsync(Arg.Any<TenantSlug>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _passwordHasher.HashPassword("SecurePass123!")
            .Returns("hashed_pass");

        _tokenGenerator.GenerateTokens(Arg.Any<Guid>(), "admin@acme.com", "Owner", Arg.Any<Guid>(), "tenant_acme_innovations")
            .Returns(("token", "refresh", DateTime.UtcNow.AddDays(7)));

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
        result.Value.AdminFullName.Should().Be("Admin Admin");
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.FirstName == "Admin" && u.LastName == "Admin"),
            Arg.Any<CancellationToken>());
    }
}
