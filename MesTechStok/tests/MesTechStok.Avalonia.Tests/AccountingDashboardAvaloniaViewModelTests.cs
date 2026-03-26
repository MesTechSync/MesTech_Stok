using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class AccountingDashboardAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AccountingDashboardAvaloniaViewModel _sut;

    public AccountingDashboardAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new AccountingDashboardAvaloniaViewModel(_mediatorMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<AccountingDashboardAvaloniaViewModel>>().Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.TotalRevenue.Should().Contain("0,00 TL");
        _sut.TotalExpense.Should().Contain("0,00 TL");
        _sut.NetProfit.Should().Contain("0,00 TL");
        _sut.Balance.Should().Contain("0,00 TL");
        _sut.LastUpdated.Should().Be("--:--");
        _sut.RecentTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_Success_ShouldPopulateKPIsAndTransactions()
    {
        // Arrange
        var summary = new MonthlySummaryDto
        {
            TotalSales = 50000m,
            TotalExpenses = 10000m,
            TotalCommissions = 5000m,
            TotalShippingCost = 3000m,
            SalesByPlatform = new List<PlatformSalesDto>
            {
                new() { Platform = "Trendyol", Sales = 30000m },
                new() { Platform = "Hepsiburada", Sales = 20000m }
            }
        };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetMonthlySummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        await _sut.LoadAsync();

        // Assert — revenue=50000, expense=10000+5000+3000=18000, profit=32000
        _sut.TotalRevenue.Should().Contain("50");
        _sut.TotalExpense.Should().Contain("18");
        _sut.NetProfit.Should().Contain("32");
        _sut.RecentTransactions.Should().HaveCount(4); // 2 platform sales + shipping + commission
        _sut.RecentTransactions.Should().Contain(t => t.Category == "Satis" && t.Description.Contains("Trendyol"));
        _sut.RecentTransactions.Should().Contain(t => t.Category == "Kargo");
        _sut.RecentTransactions.Should().Contain(t => t.Category == "Komisyon");
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingStates()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetMonthlySummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MonthlySummaryDto { SalesByPlatform = [] });

        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AccountingDashboardAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true, "IsLoading should transition to true");
        _sut.IsLoading.Should().BeFalse("IsLoading should be false after completion");
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorThrows_ShouldSetErrorState()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetMonthlySummaryQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Contain("Muhasebe verileri yuklenemedi");
        _sut.ErrorMessage.Should().Contain("DB connection failed");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WithZeroShippingAndCommission_ShouldNotAddExpenseTransactions()
    {
        // Arrange
        var summary = new MonthlySummaryDto
        {
            TotalSales = 10000m,
            TotalExpenses = 5000m,
            TotalCommissions = 0m,
            TotalShippingCost = 0m,
            SalesByPlatform = new List<PlatformSalesDto>
            {
                new() { Platform = "N11", Sales = 10000m }
            }
        };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetMonthlySummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        await _sut.LoadAsync();

        // Assert — only 1 platform sale, no kargo/komisyon rows
        _sut.RecentTransactions.Should().HaveCount(1);
        _sut.RecentTransactions.Should().NotContain(t => t.Category == "Kargo");
        _sut.RecentTransactions.Should().NotContain(t => t.Category == "Komisyon");
    }
}
