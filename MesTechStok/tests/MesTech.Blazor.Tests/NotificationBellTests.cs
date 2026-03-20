using Bunit;
using FluentAssertions;
using MesTech.Blazor.Components.Shared;
using MesTech.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Blazor.Tests;

public class NotificationBellTests : TestContext
{
    private readonly BlazorNotificationService _svc = new();

    public NotificationBellTests()
    {
        Services.AddSingleton<IBlazorNotificationService>(_svc);
    }

    [Fact]
    public void RendersBellIcon_WithoutBadge_WhenEmpty()
    {
        var cut = RenderComponent<NotificationBell>();

        cut.Find(".fa-bell").Should().NotBeNull();
        cut.FindAll(".badge").Should().BeEmpty();
    }

    [Fact]
    public void ShowsBadge_WhenUnreadNotificationsExist()
    {
        _svc.Push("Test", "Mesaj 1");
        _svc.Push("Test", "Mesaj 2");

        var cut = RenderComponent<NotificationBell>();
        var badge = cut.Find(".badge");
        badge.TextContent.Should().Be("2");
    }

    [Fact]
    public void ShowsNinePlus_WhenMoreThanNineUnread()
    {
        for (var i = 0; i < 12; i++)
            _svc.Push("Test", $"Mesaj {i}");

        var cut = RenderComponent<NotificationBell>();
        cut.Find(".badge").TextContent.Should().Be("9+");
    }

    [Fact]
    public void ShowsEmptyMessage_WhenNoNotifications()
    {
        var cut = RenderComponent<NotificationBell>();
        cut.Markup.Should().Contain("Bildirim bulunmuyor");
    }

    [Fact]
    public void RendersNotificationItems_WhenPushed()
    {
        _svc.Push("Siparis", "Yeni siparis geldi", "fa-shopping-cart", "success");

        var cut = RenderComponent<NotificationBell>();
        cut.Markup.Should().Contain("Siparis");
        cut.Markup.Should().Contain("Yeni siparis geldi");
    }

    [Fact]
    public void ClearRemovesAllNotifications()
    {
        _svc.Push("Test", "Mesaj");
        _svc.Clear();

        var cut = RenderComponent<NotificationBell>();
        cut.FindAll(".badge").Should().BeEmpty();
        cut.Markup.Should().Contain("Bildirim bulunmuyor");
    }

    [Fact]
    public void MarkAllReadResetsBadge()
    {
        _svc.Push("Test", "Mesaj");
        _svc.MarkAllRead();

        var cut = RenderComponent<NotificationBell>();
        cut.FindAll(".badge").Should().BeEmpty();
    }
}
