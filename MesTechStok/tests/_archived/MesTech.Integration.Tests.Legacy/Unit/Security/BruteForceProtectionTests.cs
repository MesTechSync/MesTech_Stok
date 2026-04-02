using FluentAssertions;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Security;

/// <summary>
/// BruteForceProtectionService integration tests — İ-18 G-01.
/// Tests: account lockout, progressive delay, IP rate limiting, success reset.
/// </summary>
public class BruteForceProtectionTests
{
    private static BruteForceProtectionService CreateService()
    {
        var tracker = new LoginAttemptTracker();
        var logger = NullLogger<BruteForceProtectionService>.Instance;
        return new BruteForceProtectionService(tracker, logger);
    }

    [Fact]
    public async Task FirstCheck_ShouldNotBeLocked()
    {
        var svc = CreateService();
        var check = await svc.CheckAsync("user1", "1.2.3.4");

        check.IsLocked.Should().BeFalse();
        check.IsIpRateLimited.Should().BeFalse();
    }

    [Fact]
    public async Task FiveFailures_ShouldLockAccount()
    {
        var svc = CreateService();
        BruteForceFailureResult result = null!;

        for (int i = 0; i < 5; i++)
            result = await svc.RecordFailureAsync("user1", "1.2.3.4");

        result.IsNowLocked.Should().BeTrue();
        result.AttemptsRemaining.Should().Be(0);
        result.LockoutDuration.Should().NotBeNull();
    }

    [Fact]
    public async Task LockedAccount_ShouldReturnIsLocked()
    {
        var svc = CreateService();
        for (int i = 0; i < 5; i++)
            await svc.RecordFailureAsync("user1", "1.2.3.4");

        var check = await svc.CheckAsync("user1", "1.2.3.4");
        check.IsLocked.Should().BeTrue();
        check.LockedUntil.Should().NotBeNull();
    }

    [Fact]
    public async Task SuccessfulLogin_ShouldResetLockout()
    {
        var svc = CreateService();
        for (int i = 0; i < 3; i++)
            await svc.RecordFailureAsync("user1", "1.2.3.4");

        await svc.RecordSuccessAsync("user1", "1.2.3.4");
        var check = await svc.CheckAsync("user1", "1.2.3.4");
        check.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task ProgressiveDelay_ShouldIncrease()
    {
        var svc = CreateService();

        var r1 = await svc.RecordFailureAsync("user1", "1.2.3.4");
        r1.Delay.Should().Be(TimeSpan.Zero); // 1st attempt: 0s

        var r2 = await svc.RecordFailureAsync("user1", "1.2.3.4");
        r2.Delay.Should().Be(TimeSpan.FromSeconds(1)); // 2nd: 1s

        var r3 = await svc.RecordFailureAsync("user1", "1.2.3.4");
        r3.Delay.Should().Be(TimeSpan.FromSeconds(2)); // 3rd: 2s

        var r4 = await svc.RecordFailureAsync("user1", "1.2.3.4");
        r4.Delay.Should().Be(TimeSpan.FromSeconds(4)); // 4th: 4s
    }

    [Fact]
    public async Task IpRateLimit_ShouldBlock21stRequest()
    {
        var svc = CreateService();

        // 20 requests should pass
        for (int i = 0; i < 20; i++)
            await svc.RecordFailureAsync($"user{i}", "10.0.0.1");

        // 21st request from same IP should be rate limited
        var check = await svc.CheckAsync("user99", "10.0.0.1");
        check.IsIpRateLimited.Should().BeTrue();
    }

    [Fact]
    public async Task DifferentIps_ShouldNotAffectEachOther()
    {
        var svc = CreateService();

        for (int i = 0; i < 20; i++)
            await svc.RecordFailureAsync($"user{i}", "10.0.0.1");

        var check = await svc.CheckAsync("userX", "10.0.0.2");
        check.IsIpRateLimited.Should().BeFalse();
    }
}
