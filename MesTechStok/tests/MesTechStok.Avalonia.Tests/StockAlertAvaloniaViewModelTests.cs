using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetStockAlerts;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockAlertAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    public StockAlertAvaloniaViewModelTests()
    {
        _currentUserMock.Setup(c => c.TenantId).Returns(Guid.NewGuid());
        // 8 alerts: 3 OutOfStock (stock<=0), 2 Critical (stock<=min), 3 Low (stock>min but still alerted)
        var alerts = new List<StockAlertDto>
        {
            new() { SKU = "SKU-001", Name = "Urun A", CurrentStock = 0, MinThreshold = 10, Platform = "Ana Depo" },
            new() { SKU = "SKU-002", Name = "Urun B", CurrentStock = 0, MinThreshold = 5, Platform = "Ana Depo" },
            new() { SKU = "SKU-003", Name = "Urun C", CurrentStock = -2, MinThreshold = 3, Platform = "Yedek Depo" },
            new() { SKU = "SKU-004", Name = "Urun D", CurrentStock = 2, MinThreshold = 10, Platform = "Ana Depo" },
            new() { SKU = "SKU-005", Name = "Urun E", CurrentStock = 1, MinThreshold = 5, Platform = "Yedek Depo" },
            new() { SKU = "SKU-006", Name = "Urun F", CurrentStock = 8, MinThreshold = 5, Platform = "Ana Depo" },
            new() { SKU = "SKU-007", Name = "Urun G", CurrentStock = 12, MinThreshold = 10, Platform = "Ana Depo" },
            new() { SKU = "SKU-008", Name = "Urun H", CurrentStock = 15, MinThreshold = 8, Platform = "Yedek Depo" },
        };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetStockAlertsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);
    }

    private StockAlertAvaloniaViewModel CreateSut()
        => new(_mediatorMock.Object, _currentUserMock.Object);

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.CurrentFilter.Should().Be("All");
        sut.FilteredAlerts.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetIsLoadingAndPopulateAlerts()
    {
        var sut = CreateSut();

        await sut.LoadAsync();

        sut.IsLoading.Should().BeFalse();
        sut.FilteredAlerts.Should().NotBeEmpty();
        sut.IsEmpty.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.AlertSummary.Should().Contain("tukendi");
        sut.AlertSummary.Should().Contain("kritik");
    }

    [Fact]
    public async Task LoadAsync_FilterByOutOfStock_ShouldShowOnlyOutOfStockAlerts()
    {
        var sut = CreateSut();
        await sut.LoadAsync();
        var totalBefore = sut.FilteredAlerts.Count;

        sut.FilterOutOfStockCommand.Execute(null);

        sut.FilteredAlerts.Should().NotBeEmpty();
        sut.FilteredAlerts.Count.Should().BeLessThan(totalBefore);
        sut.FilteredAlerts.Should().OnlyContain(a => a.Level == "OutOfStock");
        sut.CurrentFilter.Should().Be("OutOfStock");
    }

    [Fact]
    public async Task LoadAsync_AlertSummary_ShouldContainCorrectCounts()
    {
        var sut = CreateSut();

        await sut.LoadAsync();

        // 3 OutOfStock (stock<=0: SKU-001,002,003), 2 Critical (stock<=min: SKU-004,005), 3 Low (rest)
        sut.AlertSummary.Should().Be("3 tukendi | 2 kritik | 3 dusuk");
        sut.FilteredAlerts.Should().HaveCount(8);
    }

    [Fact]
    public async Task LoadAsync_FilterAll_ShouldShowAllAlerts()
    {
        var sut = CreateSut();
        await sut.LoadAsync();

        sut.FilterOutOfStockCommand.Execute(null);
        var filteredCount = sut.FilteredAlerts.Count;

        sut.FilterAllCommand.Execute(null);

        sut.FilteredAlerts.Count.Should().BeGreaterThan(filteredCount);
        sut.CurrentFilter.Should().Be("All");
    }
}
