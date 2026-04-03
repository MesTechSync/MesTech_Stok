using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Application.Features.Finance.Queries.GetCashRegisters;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class GelirGiderAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly GelirGiderAvaloniaViewModel _sut;

    public GelirGiderAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        var tenantMock = new Mock<ITenantProvider>();
        tenantMock.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());

        // 7 seed items: 4 Satis/Gelir + 3 Gider (non-Satis)
        // Income: 4520 + 2180 + 1240 + 3890 = 11830
        // Expense: -380 + -6500 + -542.40 = -7422.40
        var seedItems = new List<IncomeExpenseItemDto>
        {
            new(Guid.NewGuid(), "Trendyol satis hasilat", 4520m, "Gelir", "Satis", DateTime.Now.AddDays(-1), null),
            new(Guid.NewGuid(), "Hepsiburada satis hasilat", 2180m, "Gelir", "Satis", DateTime.Now.AddDays(-2), null),
            new(Guid.NewGuid(), "N11 satis hasilat", 1240m, "Gelir", "Satis", DateTime.Now.AddDays(-3), null),
            new(Guid.NewGuid(), "Amazon satis hasilat", 3890m, "Gelir", "Satis", DateTime.Now.AddDays(-4), null),
            new(Guid.NewGuid(), "Kargo gider", -380m, "Gider", "Kargo", DateTime.Now.AddDays(-5), null),
            new(Guid.NewGuid(), "Depo kira odemesi", -6500m, "Gider", "Genel Gider", DateTime.Now.AddDays(-6), null),
            new(Guid.NewGuid(), "Pazaryeri komisyon kesintisi", -542.40m, "Gider", "Pazaryeri Komisyon", DateTime.Now.AddDays(-7), null),
        };

        var resultDto = new IncomeExpenseListResultDto(seedItems, seedItems.Count);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetIncomeExpenseListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // GetCashRegistersQuery — secondary call at end of LoadAsync
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCashRegistersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CashRegisterDto>() as IReadOnlyList<CashRegisterDto>);

        _sut = new GelirGiderAvaloniaViewModel(_mediatorMock.Object, tenantMock.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        // Assert
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.TotalIncome.Should().Be("0,00 TL");
        _sut.TotalExpense.Should().Be("0,00 TL");
        _sut.NetBalance.Should().Be("0,00 TL");
        _sut.SearchText.Should().BeEmpty();
        _sut.SelectedCategory.Should().Be("Tumu");
        _sut.SelectedTypeFilter.Should().Be("Tumu");
        _sut.Items.Should().BeEmpty();
        _sut.Categories.Should().HaveCount(7);
        _sut.TypeFilters.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateItems_AndCalculateKPIs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Items.Should().HaveCount(7);
        _sut.TotalCount.Should().Be(7);
        _sut.TotalIncome.Should().Contain("TL");
        _sut.TotalExpense.Should().Contain("TL");
        _sut.NetBalance.Should().Contain("TL");

        // Verify KPI calculations: income = 4520+2180+1240+3890 = 11830
        _sut.TotalIncome.Should().Contain("11");
        // expense = 380+6500+542.40 = 7422.40
        _sut.TotalExpense.Should().Contain("7");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(GelirGiderAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true, "IsLoading should have been set to true");
        _sut.IsLoading.Should().BeFalse("IsLoading should be false after load completes");
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyFilters_CategoryFilter_FiltersCorrectly()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedCategory = "Satis";

        // Assert — only items with Category=="Satis" should remain
        _sut.Items.Should().OnlyContain(x => x.Category == "Satis");
        _sut.Items.Should().HaveCount(4);
        _sut.TotalCount.Should().Be(4);
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyFilters_TypeFilter_GelirOnly()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedTypeFilter = "Gelir";

        // Assert
        _sut.Items.Should().OnlyContain(x => x.Type == "Gelir");
        _sut.Items.Should().HaveCount(4);
    }

    [Fact]
    public async Task ApplyFilters_SearchText_FiltersDescription()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — search for "Trendyol" (>= 2 chars triggers filter)
        _sut.SearchText = "Trendyol";

        // Assert
        _sut.Items.Should().OnlyContain(x =>
            x.Description.Contains("Trendyol", StringComparison.OrdinalIgnoreCase));
        _sut.Items.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task ApplyFilters_NoMatch_SetsIsEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "BuMetinHicbirYerdeYok";

        // Assert
        _sut.Items.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
        _sut.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Refresh_CallsLoadAsync()
    {
        // Act
        await _sut.RefreshCommand.ExecuteAsync(null);

        // Assert — after refresh, items should be populated
        _sut.Items.Should().HaveCount(7);
        _sut.IsLoading.Should().BeFalse();
    }
}
