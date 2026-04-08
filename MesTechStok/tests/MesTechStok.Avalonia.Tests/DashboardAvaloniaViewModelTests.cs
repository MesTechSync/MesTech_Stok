using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs.Dashboard;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Application.Features.Dashboard.Queries.GetSalesChartData;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class DashboardAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITenantProvider> _tenantMock = new();

    private DashboardAvaloniaViewModel CreateSut()
    {
        _tenantMock.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetSalesChartDataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesChartDataDto());
        return new DashboardAvaloniaViewModel(_mediatorMock.Object, _tenantMock.Object);
    }

    private DashboardSummaryDto CreateSummary(int products = 5, int orders = 3, decimal revenue = 1500m,
        int critical = 2, int platforms = 3, int pending = 1, decimal monthly = 5000m, decimal returnRate = 1.5m)
    {
        return new DashboardSummaryDto
        {
            ActiveProductCount = products,
            TodayOrderCount = orders,
            TodaySalesAmount = revenue,
            CriticalStockCount = critical,
            ActivePlatformCount = platforms,
            PendingShipmentCount = pending,
            MonthlySalesAmount = monthly,
            ReturnRate = returnRate,
            RecentOrders = new List<RecentOrderItemDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "SIP-001",
                    CustomerName = "Test Customer",
                    TotalAmount = 500m,
                    Status = "Yeni",
                    PlatformName = "Trendyol",
                    CreatedAt = DateTime.Now
                }
            },
            CriticalStockItems = new List<CriticalStockItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Test Product",
                    SKU = "TST-001",
                    CurrentStock = 2,
                    MinimumStock = 10
                }
            }
        };
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        using var sut = CreateSut();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.TotalProducts.Should().Be("0");
        sut.TodayOrderCount.Should().Be("0");
        sut.TodayRevenue.Should().Be("0 TL");
        sut.CriticalStockCount.Should().Be("0");
        sut.RecentOrders.Should().BeEmpty();
        sut.CriticalStockItems.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetIsLoadingDuringExecution()
    {
        // Arrange
        var tcs = new TaskCompletionSource<DashboardSummaryDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDashboardSummaryQuery>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        using var sut = CreateSut();
        var loadTask = sut.LoadAsync();

        // Assert — loading state while awaiting
        sut.IsLoading.Should().BeTrue();
        sut.HasError.Should().BeFalse();

        // Complete
        tcs.SetResult(CreateSummary());
        await loadTask;

        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateKpiAndCollections()
    {
        // Arrange
        var summary = CreateSummary(products: 42, orders: 7, revenue: 2500.50m, critical: 3);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDashboardSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        using var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.TotalProducts.Should().Be(42.ToString("N0"));
        sut.TodayOrderCount.Should().Be(7.ToString("N0"));
        sut.TodayRevenue.Should().Contain("2");
        sut.CriticalStockCount.Should().Be(3.ToString("N0"));
        sut.RecentOrders.Should().HaveCount(1);
        sut.CriticalStockItems.Should().HaveCount(1);
        sut.IsEmpty.Should().BeFalse();
        sut.CriticalStockBadgeCount.Should().Be(3);
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorThrows_ShouldSetErrorState()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDashboardSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        using var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().Contain("yuklenirken hata");
        sut.ErrorMessage.Should().Contain("DB connection failed");
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenNoRecentOrders_ShouldSetEmptyState()
    {
        // Arrange
        var summary = CreateSummary();
        summary.RecentOrders = new List<RecentOrderItemDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDashboardSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        using var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsEmpty.Should().BeTrue();
        sut.RecentOrders.Should().BeEmpty();
        sut.HasError.Should().BeFalse();
    }
}
