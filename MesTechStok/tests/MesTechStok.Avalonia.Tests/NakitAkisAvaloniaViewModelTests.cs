using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

/// <summary>
/// Unit tests for NakitAkisAvaloniaViewModel.
/// Covers constructor defaults, LoadAsync KPI calculations,
/// 3-state loading pattern, filter logic, and error handling.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class NakitAkisAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly NakitAkisAvaloniaViewModel _sut;

    public NakitAkisAvaloniaViewModelTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockCurrentUser.Setup(c => c.TenantId).Returns(Guid.NewGuid());

        // 6 seed entries: 3 inflow + 3 outflow
        // Inflow: 12480 + 8920 + 3150 = 24550
        // Outflow: 1240 + 6500 + 2100 = 9840
        // Net: 24550 - 9840 = 14710
        var seedEntries = new List<CashFlowEntryDto>
        {
            new() { Id = Guid.NewGuid(), EntryDate = DateTime.Now.AddDays(-1), Amount = 12480m, Direction = "Inflow", Description = "Trendyol hakedis odemesi" },
            new() { Id = Guid.NewGuid(), EntryDate = DateTime.Now.AddDays(-2), Amount = 8920m, Direction = "Inflow", Description = "Hepsiburada hakedis odemesi" },
            new() { Id = Guid.NewGuid(), EntryDate = DateTime.Now.AddDays(-3), Amount = 3150m, Direction = "Inflow", Description = "N11 hakedis odemesi" },
            new() { Id = Guid.NewGuid(), EntryDate = DateTime.Now.AddDays(-4), Amount = 1240m, Direction = "Outflow", Description = "Kargo toplu odeme" },
            new() { Id = Guid.NewGuid(), EntryDate = DateTime.Now.AddDays(-5), Amount = 6500m, Direction = "Outflow", Description = "AWS sunucu odemesi" },
            new() { Id = Guid.NewGuid(), EntryDate = DateTime.Now.AddDays(-6), Amount = 2100m, Direction = "Outflow", Description = "Depo kira odemesi" },
        };

        var reportDto = new CashFlowReportDto
        {
            TotalInflow = 24550m,
            TotalOutflow = 9840m,
            NetFlow = 14710m,
            Entries = seedEntries
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetCashFlowReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportDto);

        // GetCashFlowTrendQuery — secondary call in LoadAsync
        var trendDto = new CashFlowTrendDto(
            new List<CashFlowMonthDto>
            {
                new("2026-01", 20000m, 8000m, 12000m),
                new("2026-02", 22000m, 9000m, 13000m),
                new("2026-03", 24550m, 9840m, 14710m),
            },
            CumulativeNet: 39710m);

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetCashFlowTrendQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trendDto);

        _sut = new NakitAkisAvaloniaViewModel(_mockMediator.Object, _mockCurrentUser.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        // Assert
        _sut.SelectedPeriodType.Should().Be("Aylik");
        _sut.SearchText.Should().BeEmpty();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.TotalInflow.Should().Be("0,00 TL");
        _sut.TotalOutflow.Should().Be("0,00 TL");
        _sut.NetCashFlow.Should().Be("0,00 TL");

        _sut.PeriodTypes.Should().HaveCount(4);
        _sut.PeriodTypes.Should().ContainInOrder("Gunluk", "Haftalik", "Aylik", "Yillik");

        _sut.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateItems()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — 6 seed items
        _sut.Items.Should().HaveCount(6);
        _sut.Items.Select(x => x.Description).Should().Contain("Trendyol hakedis odemesi");
        _sut.Items.Select(x => x.Description).Should().Contain("AWS sunucu odemesi");
        _sut.TotalCount.Should().Be(6);
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateKPIs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        // Inflow: 12480 + 8920 + 3150 = 24550
        _sut.TotalInflow.Should().Be("24.550,00 TL");

        // Outflow: 1240 + 6500 + 2100 = 9840
        _sut.TotalOutflow.Should().Be("9.840,00 TL");

        // Net: 24550 - 9840 = 14710
        _sut.NetCashFlow.Should().Be("14.710,00 TL");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        // Arrange — track loading state transitions
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_sut.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert — IsLoading should go true then false
        loadingStates.Should().ContainInOrder(true, false);
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyFilters_SearchText_ShouldFilterByDescription()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — search for Trendyol
        _sut.SearchText = "Trendyol";

        // Assert — only Trendyol hakedis odemesi
        _sut.Items.Should().HaveCount(1);
        _sut.Items[0].Description.Should().Contain("Trendyol");
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyFilters_NoMatch_ShouldSetIsEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "nonexistent";

        // Assert
        _sut.Items.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
        _sut.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_Success_ShouldClearErrorState()
    {
        _sut.HasError.Should().BeFalse();
        await _sut.LoadAsync();
        _sut.HasError.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorThrows_SetsHasError()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetCashFlowReportQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var sut = new NakitAkisAvaloniaViewModel(mediator.Object, _mockCurrentUser.Object);
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().NotBeNullOrEmpty();
        sut.IsLoading.Should().BeFalse(); // KC-13
    }

    [Fact]
    public async Task Refresh_ShouldCallLoadAsync()
    {
        // Act — call Refresh command which delegates to LoadAsync
        await _sut.RefreshCommand.ExecuteAsync(null);

        // Assert — items should be populated (proving LoadAsync ran)
        _sut.Items.Should().HaveCount(6);
        _sut.IsLoading.Should().BeFalse();
        _sut.TotalInflow.Should().NotBe("0,00 TL");
    }
}
