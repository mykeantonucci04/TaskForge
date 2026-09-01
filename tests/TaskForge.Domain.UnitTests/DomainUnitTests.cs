using FluentAssertions;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Events;
using TaskForge.Shared.Exceptions;
using Xunit;

namespace TaskForge.Domain.UnitTests;

public class DomainUnitTests
{
    // ==========================================
    // 1. Value Object: Email Tests
    // ==========================================

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name+tag@sub.domain.org")]
    [InlineData("admin123@taskforge.io")]
    public void Email_Create_Should_Succeed_For_Valid_Email(string validEmail)
    {
        var email = Email.Create(validEmail);
        email.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    [Fact]
    public void Email_Create_Should_Normalize_To_Lowercase()
    {
        var email = Email.Create("ADMIN@TaskForge.IO");
        email.Value.Should().Be("admin@taskforge.io");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@missinguser.com")]
    [InlineData("missingdomain@")]
    public void Email_Create_Should_Throw_DomainException_For_Invalid_Email(string? invalidEmail)
    {
        var act = () => Email.Create(invalidEmail);
        act.Should().Throw<DomainException>();
    }

    // ==========================================
    // 2. Value Object: TenantSlug Tests
    // ==========================================

    [Theory]
    [InlineData("acme")]
    [InlineData("acme-corp")]
    [InlineData("company-123-prod")]
    public void TenantSlug_Create_Should_Succeed_For_Valid_Slug(string validSlug)
    {
        var slug = TenantSlug.Create(validSlug);
        slug.Value.Should().Be(validSlug.ToLowerInvariant());
    }

    [Fact]
    public void TenantSlug_Create_Should_Normalize_To_Lowercase()
    {
        var slug = TenantSlug.Create("Acme-Corp");
        slug.Value.Should().Be("acme-corp");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")] // Too short (< 3)
    [InlineData("-acme")] // Starts with hyphen
    [InlineData("acme-")] // Ends with hyphen
    [InlineData("acme--corp")] // Consecutive hyphens
    [InlineData("acme_corp")] // Underscore not allowed in slug
    [InlineData("acme corp")] // Space not allowed
    public void TenantSlug_Create_Should_Throw_DomainException_For_Invalid_Slug(string? invalidSlug)
    {
        var act = () => TenantSlug.Create(invalidSlug);
        act.Should().Throw<DomainException>();
    }

    // ==========================================
    // 3. Entity: Tenant Tests
    // ==========================================

    [Fact]
    public void Tenant_Create_Should_Create_Active_Tenant_And_Emit_Event()
    {
        // Arrange
        var slug = TenantSlug.Create("acme-corp");

        // Act
        var tenant = Tenant.Create("Acme Corporation", slug, TenantPlan.Pro, "creator@acme.com");

        // Assert
        tenant.Id.Should().NotBeEmpty();
        tenant.Name.Should().Be("Acme Corporation");
        tenant.Slug.Should().Be(slug);
        tenant.Plan.Should().Be(TenantPlan.Pro);
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.SchemaName.Should().Be("tenant_acme_corp");
        tenant.CreatedBy.Should().Be("creator@acme.com");

        tenant.DomainEvents.Should().ContainSingle();
        var domainEvent = tenant.DomainEvents.First().Should().BeOfType<TenantCreatedEvent>().Subject;
        domainEvent.TenantId.Should().Be(tenant.Id);
        domainEvent.Name.Should().Be(tenant.Name);
        domainEvent.Slug.Should().Be("acme-corp");
        domainEvent.SchemaName.Should().Be("tenant_acme_corp");
        domainEvent.Plan.Should().Be(TenantPlan.Pro);
    }

    [Fact]
    public void Tenant_Create_Should_Throw_When_Name_Is_Empty()
    {
        var slug = TenantSlug.Create("acme-corp");
        var act = () => Tenant.Create("", slug);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Tenant_Status_Transitions_Should_Work()
    {
        var slug = TenantSlug.Create("acme-corp");
        var tenant = Tenant.Create("Acme Corporation", slug);

        tenant.Suspend();
        tenant.Status.Should().Be(TenantStatus.Suspended);

        tenant.Activate();
        tenant.Status.Should().Be(TenantStatus.Active);

        tenant.Cancel();
        tenant.Status.Should().Be(TenantStatus.Cancelled);
    }

    // ==========================================
    // 4. Entity: User Tests
    // ==========================================

    [Fact]
    public void User_Create_Should_Create_Active_User_With_Full_Name()
    {
        // Arrange
        var email = Email.Create("john.doe@example.com");

        // Act
        var user = User.Create(email, "hashed_password_123", "John", "Doe");

        // Assert
        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be(email);
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.FullName.Should().Be("John Doe");
        user.IsActive.Should().BeTrue();
        user.PasswordHash.Should().Be("hashed_password_123");
    }

    [Fact]
    public void User_AddRefreshToken_Should_Add_Valid_Token()
    {
        var email = Email.Create("john.doe@example.com");
        var user = User.Create(email, "hash", "John", "Doe");

        var token = user.AddRefreshToken("refresh_token_abc", DateTime.UtcNow.AddDays(7), "127.0.0.1");

        token.Should().NotBeNull();
        token.Token.Should().Be("refresh_token_abc");
        token.IsActive.Should().BeTrue();
        token.IsExpired.Should().BeFalse();
        user.RefreshTokens.Should().Contain(token);
    }

    // ==========================================
    // 5. Entity: TenantMembership Tests
    // ==========================================

    [Fact]
    public void TenantMembership_Create_Should_Set_Owner_Role()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var membership = TenantMembership.Create(tenantId, userId, MembershipRole.Owner, MembershipStatus.Active);

        membership.Id.Should().NotBeEmpty();
        membership.TenantId.Should().Be(tenantId);
        membership.UserId.Should().Be(userId);
        membership.Role.Should().Be(MembershipRole.Owner);
        membership.Status.Should().Be(MembershipStatus.Active);
        membership.JoinedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void TenantMembership_ChangeRole_Should_Update_Role()
    {
        var membership = TenantMembership.Create(Guid.NewGuid(), Guid.NewGuid(), MembershipRole.Member);
        membership.ChangeRole(MembershipRole.Admin);

        membership.Role.Should().Be(MembershipRole.Admin);
    }

    // ==========================================
    // 6. Entity: TenantInvitation Tests
    // ==========================================

    [Fact]
    public void TenantInvitation_Create_And_Accept_Should_Succeed()
    {
        var tenantId = Guid.NewGuid();
        var invitedById = Guid.NewGuid();
        var email = Email.Create("colleague@example.com");

        var invitation = TenantInvitation.Create(tenantId, email, MembershipRole.Member, invitedById);

        invitation.Token.Should().NotBeNullOrWhiteSpace();
        invitation.Status.Should().Be(MembershipStatus.Pending);
        invitation.IsExpired(DateTime.UtcNow).Should().BeFalse();

        invitation.Accept();
        invitation.Status.Should().Be(MembershipStatus.Active);
    }

    // ==========================================
    // 7. Entity: RefreshToken Tests
    // ==========================================

    [Fact]
    public void RefreshToken_Revoke_Should_Deactivate_Token()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token_123", DateTime.UtcNow.AddDays(7));
        token.IsActive.Should().BeTrue();

        token.Revoke("127.0.0.1", "replacement_token_456");

        token.IsActive.Should().BeFalse();
        token.IsRevoked.Should().BeTrue();
        token.RevokedAtUtc.Should().NotBeNull();
        token.ReplacedByToken.Should().Be("replacement_token_456");
    }
}
