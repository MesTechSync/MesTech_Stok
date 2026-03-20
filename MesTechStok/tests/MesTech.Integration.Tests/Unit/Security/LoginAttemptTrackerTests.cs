using FluentAssertions;
using MesTech.Infrastructure.Security;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Security;

public class LoginAttemptTrackerTests
{
    [Fact]
    public void FirstAttempt_ShouldNotBeLocked()
    {
        var tracker = new LoginAttemptTracker();
        var (isLocked, _) = tracker.CheckLockout("testuser");
        isLocked.Should().BeFalse();
    }

    [Fact]
    public void FiveFailedAttempts_ShouldLockout()
    {
        var tracker = new LoginAttemptTracker();
        for (int i = 0; i < 4; i++)
            tracker.RecordFailedAttempt("testuser");

        var (locked, _, lockoutDuration) = tracker.RecordFailedAttempt("testuser");
        locked.Should().BeTrue();
        lockoutDuration.Should().NotBeNull();
        lockoutDuration!.Value.TotalSeconds.Should().BeGreaterOrEqualTo(60);
    }

    [Fact]
    public void SuccessfulLogin_ShouldResetCounter()
    {
        var tracker = new LoginAttemptTracker();
        for (int i = 0; i < 3; i++)
            tracker.RecordFailedAttempt("testuser");

        tracker.RecordSuccess("testuser");
        var (isLocked, _) = tracker.CheckLockout("testuser");
        isLocked.Should().BeFalse();
    }

    [Fact]
    public void DifferentUsers_ShouldHaveSeparateCounters()
    {
        var tracker = new LoginAttemptTracker();
        for (int i = 0; i < 4; i++)
            tracker.RecordFailedAttempt("user1");

        var (isLocked, _) = tracker.CheckLockout("user2");
        isLocked.Should().BeFalse();
    }

    [Fact]
    public void CaseInsensitive_ShouldTrackSameUser()
    {
        var tracker = new LoginAttemptTracker();
        tracker.RecordFailedAttempt("Admin");
        tracker.RecordFailedAttempt("admin");
        tracker.RecordFailedAttempt("ADMIN");
        tracker.RecordFailedAttempt("aDmIn");

        var (locked, _, _) = tracker.RecordFailedAttempt("admin");
        locked.Should().BeTrue();
    }
}
