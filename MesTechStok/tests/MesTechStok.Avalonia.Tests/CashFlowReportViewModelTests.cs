using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CashFlowReportViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly CashFlowReportViewModel _sut;

    public CashFlowReportViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCashFlowReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CashFlowReportDto
            {
                TotalInflow = 245_800.00m,
                TotalOutflow = 178_350.00m,
                NetFlow = 67_450.00m,
                Entries = new List<CashFlowEntryDto>
                {
                    new() { Id = Guid.NewGuid(), EntryDate = new DateTime(2026, 1, 15), Amount = 82_500.00m, Direction = "Inflow", Category = "Trendyol Satis", Description = "Ocak Trendyol hasilat" },
                    new() { Id = Guid.NewGuid(), EntryDate = new DateTime(2026, 1, 20), Amount = 54_200.00m, Direction = "Outflow", Category = "Tedarikci Odeme", Description = "Ocak mal alimi" },
                    new() { Id = Guid.NewGuid(), EntryDate = new DateTime(2026, 2, 10), Amount = 78_300.00m, Direction = "Inflow", Category = "Hepsiburada Satis", Description = "Subat Hepsiburada hasilat" },
                    new() { Id = Guid.NewGuid(), EntryDate = new DateTime(2026, 2, 18), Amount = 61_150.00m, Direction = "Outflow", Category = "Kargo Gideri", Description = "Subat kargo odemesi" },
                    new() { Id = Guid.NewGuid(), EntryDate = new DateTime(2026, 3, 5), Amount = 85_000.00m, Direction = "Inflow", Category = "Amazon Satis", Description = "Mart Amazon hasilat" },
                    new() { Id = Guid.NewGuid(), EntryDate = new DateTime(2026, 3, 12), Amount = 63_000.00m, Direction = "Outflow", Category = "Komisyon", Description = "Mart platform komisyonlari" },
                }
            });
        _sut = new CashFlowReportViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.Title.Should().Be("Nakit Akis Raporu");
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.MonthlyFlows.Should().BeEmpty();
        _sut.TotalInflowText.Should().Contain("TL");
        _sut.TotalOutflowText.Should().Contain("TL");
        _sut.NetFlowText.Should().Contain("TL");
    }

    [Fact]
    public void Constructor_ShouldSetDefaultDateRange()
    {
        _sut.DateFrom.Should().NotBeNull();
        _sut.DateTo.Should().NotBeNull();
        _sut.DateFrom!.Value.Year.Should().Be(DateTime.Now.Year);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateMonthlyFlows()
    {
        await _sut.LoadAsync();

        _sut.MonthlyFlows.Should().NotBeEmpty();
        _sut.MonthlyFlows.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateTotals()
    {
        await _sut.LoadAsync();

        _sut.TotalInflowText.Should().NotBe("0.00 TL");
        _sut.TotalOutflowText.Should().NotBe("0.00 TL");
        _sut.NetFlowText.Should().NotBe("0.00 TL");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CashFlowReportViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        await _sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        _sut.HasError.Should().BeFalse();
    }
}
