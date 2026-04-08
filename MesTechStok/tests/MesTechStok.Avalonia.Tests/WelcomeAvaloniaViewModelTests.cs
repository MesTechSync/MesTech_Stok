using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs.Dashboard;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class WelcomeAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUserService> _userMock = new();

    public WelcomeAvaloniaViewModelTests()
    {
        _userMock.Setup(u => u.TenantId).Returns(Guid.NewGuid());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDashboardSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSummaryDto
            {
                ActiveProductCount = 3284,
                TodayOrderCount = 156,
                ActivePlatformCount = 5,
                RecentOrders = new List<RecentOrderItemDto>
                {
                    new() { OrderNumber = "TY-001", CustomerName = "Trendyol Musteri", TotalAmount = 299.99m, CreatedAt = DateTime.Now.AddMinutes(-5) },
                    new() { OrderNumber = "HB-002", CustomerName = "HB Musteri", TotalAmount = 150m, CreatedAt = DateTime.Now.AddHours(-1) },
                    new() { OrderNumber = "N11-003", CustomerName = "N11 Musteri", TotalAmount = 89m, CreatedAt = DateTime.Now.AddHours(-3) }
                }
            });
    }

    private WelcomeAvaloniaViewModel CreateSut() => new(_mediatorMock.Object, _userMock.Object);

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.WelcomeText.Should().Be("Entegrator Stok Yonetim Sistemi");
        sut.TotalProducts.Should().Be("-");
        sut.TotalOrders.Should().Be("-");
        sut.ActivePlatforms.Should().Be("-");
        sut.RecentActivities.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateKPIValues()
    {
        var sut = CreateSut();
        await sut.LoadAsync();

        sut.TotalProducts.Should().MatchRegex(@"3[\.,]284");
        sut.TotalOrders.Should().Be("156");
        sut.ActivePlatforms.Should().Be("5");
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateRecentActivities()
    {
        var sut = CreateSut();
        await sut.LoadAsync();

        sut.RecentActivities.Should().HaveCount(3);
        sut.RecentActivities[0].Description.Should().Contain("TY-001");
    }

    [Fact]
    public async Task LoadAsync_3StateTransition_ShouldEndInSuccessState()
    {
        var sut = CreateSut();
        await sut.LoadAsync();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_RepeatedCalls_ShouldNotDoubleUp()
    {
        var sut = CreateSut();
        await sut.LoadAsync();
        sut.RecentActivities.Should().HaveCount(3);

        await sut.LoadAsync();
        sut.RecentActivities.Should().HaveCount(3);
        sut.TotalProducts.Should().MatchRegex(@"3[\.,]284");
    }
}
