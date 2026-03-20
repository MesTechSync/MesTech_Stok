using FluentAssertions;
using MesTech.Blazor.Services;

namespace MesTech.Blazor.Tests;

public class BlazorNotificationServiceTests
{
    [Fact]
    public void Push_AddsNotification_AndIncrementsUnread()
    {
        var svc = new BlazorNotificationService();
        svc.Push("Baslik", "Mesaj");

        svc.Notifications.Should().HaveCount(1);
        svc.UnreadCount.Should().Be(1);
        svc.Notifications[0].Title.Should().Be("Baslik");
    }

    [Fact]
    public void Push_InsertsAtFront_NewestFirst()
    {
        var svc = new BlazorNotificationService();
        svc.Push("Eski", "1");
        svc.Push("Yeni", "2");

        svc.Notifications[0].Title.Should().Be("Yeni");
    }

    [Fact]
    public void Push_LimitsToFifty()
    {
        var svc = new BlazorNotificationService();
        for (var i = 0; i < 60; i++)
            svc.Push($"N{i}", $"M{i}");

        svc.Notifications.Should().HaveCount(50);
    }

    [Fact]
    public void MarkAllRead_ResetsUnreadCount()
    {
        var svc = new BlazorNotificationService();
        svc.Push("A", "B");
        svc.Push("C", "D");
        svc.MarkAllRead();

        svc.UnreadCount.Should().Be(0);
        svc.Notifications.Should().HaveCount(2);
    }

    [Fact]
    public void Clear_RemovesAllAndResetsCount()
    {
        var svc = new BlazorNotificationService();
        svc.Push("A", "B");
        svc.Clear();

        svc.Notifications.Should().BeEmpty();
        svc.UnreadCount.Should().Be(0);
    }

    [Fact]
    public void OnChange_FiresOnPush()
    {
        var svc = new BlazorNotificationService();
        var fired = false;
        svc.OnChange += () => fired = true;

        svc.Push("X", "Y");
        fired.Should().BeTrue();
    }

    [Fact]
    public void OnChange_FiresOnMarkAllRead()
    {
        var svc = new BlazorNotificationService();
        svc.Push("X", "Y");

        var fired = false;
        svc.OnChange += () => fired = true;
        svc.MarkAllRead();
        fired.Should().BeTrue();
    }

    [Fact]
    public void OnChange_FiresOnClear()
    {
        var svc = new BlazorNotificationService();
        svc.Push("X", "Y");

        var fired = false;
        svc.OnChange += () => fired = true;
        svc.Clear();
        fired.Should().BeTrue();
    }
}
