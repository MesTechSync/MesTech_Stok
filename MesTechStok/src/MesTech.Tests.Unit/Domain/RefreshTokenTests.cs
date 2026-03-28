using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

// ════════════════════════════════════════════════════════
// DEV5 TUR 12: RefreshToken domain entity tests (G067/G225)
// OWASP ASVS V3.3: Token rotation, revoke, expiry
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Security", "RefreshToken")]
public class RefreshTokenTests
{
    [Fact]
    public void Create_ShouldSetCorrectFields()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var token = RefreshToken.Create(userId, tenantId, "hash123", 30, "127.0.0.1", "TestAgent");

        token.UserId.Should().Be(userId);
        token.TenantId.Should().Be(tenantId);
        token.TokenHash.Should().Be("hash123");
        token.IpAddress.Should().Be("127.0.0.1");
        token.UserAgent.Should().Be("TestAgent");
        token.IsRevoked.Should().BeFalse();
        token.RevokedAt.Should().BeNull();
        token.ReplacedByTokenHash.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetExpiryInFuture()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", 30, null, null);

        token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void IsActive_NewToken_ShouldBeTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", 30, null, null);

        token.IsActive.Should().BeTrue();
        token.IsExpired.Should().BeFalse();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsActive_RevokedToken_ShouldBeFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", 30, null, null);

        token.Revoke("rotation");

        token.IsActive.Should().BeFalse();
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void IsActive_ExpiredToken_ShouldBeFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", 0, null, null);
        // ExpiresAt = UtcNow + 0 days = now, IsExpired checks > so need to set in past
        token.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        token.IsActive.Should().BeFalse();
        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void Revoke_ShouldSetReasonAndTimestamp()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", 30, null, null);

        token.Revoke("reuse-detected", "newHash456");

        token.IsRevoked.Should().BeTrue();
        token.RevokedReason.Should().Be("reuse-detected");
        token.ReplacedByTokenHash.Should().Be("newHash456");
        token.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Revoke_WithoutReplacement_ShouldLeaveReplacedByNull()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", 30, null, null);

        token.Revoke("manual-revoke");

        token.ReplacedByTokenHash.Should().BeNull();
        token.RevokedReason.Should().Be("manual-revoke");
    }

    [Fact]
    public void Revoke_MultipleTimes_ShouldOverwriteReason()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", 30, null, null);

        token.Revoke("first-reason");
        var firstRevokedAt = token.RevokedAt;

        token.Revoke("second-reason", "anotherHash");

        token.RevokedReason.Should().Be("second-reason");
        token.ReplacedByTokenHash.Should().Be("anotherHash");
    }

    [Fact]
    public void Create_NullIpAndUserAgent_ShouldBeAllowed()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", 7, null, null);

        token.IpAddress.Should().BeNull();
        token.UserAgent.Should().BeNull();
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TokenHash_ShouldNeverBeEmpty()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "sha256hash", 30, null, null);

        token.TokenHash.Should().NotBeNullOrEmpty();
        token.TokenHash.Should().Be("sha256hash");
    }
}
