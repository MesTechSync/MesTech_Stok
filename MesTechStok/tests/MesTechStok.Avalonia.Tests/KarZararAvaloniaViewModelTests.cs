using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Queries.GetKarZarar;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class KarZararAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly KarZararAvaloniaViewModel _sut;

    /// <summary>
    /// Seed KarZararDto: revenue=125480, expenses=87320, profit=38160, margin~30.4%
    /// </summary>
    private static KarZararDto CreateSeedDto() => new()
    {
        ToplamGelir = 125480m,
        ToplamGider = 87320m,
        NetKar = 38160m,
        DönemBasi = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
        DönemSonu = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1)
    };

    public KarZararAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockCurrentUser.Setup(x => x.TenantId).Returns(Guid.NewGuid());

        // Default setup: return populated KarZararDto
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetKarZararQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSeedDto());

        _sut = new KarZararAvaloniaViewModel(_mediatorMock.Object, _mockCurrentUser.Object);
    }

    [Fact]
    public void Constructor_ShouldSetPeriodLabel()
    {
        // Assert — PeriodLabel should contain current month in Turkish and year
        var now = DateTime.Now;
        var turkishMonths = new[] { "", "Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran",
            "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik" };
        var expectedMonth = turkishMonths[now.Month];

        _sut.PeriodLabel.Should().Contain(expectedMonth);
        _sut.PeriodLabel.Should().Contain(now.Year.ToString());
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.LineItems.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateLineItems()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — ViewModel creates 2 summary lines: Toplam Gelir + Toplam Gider
        _sut.LineItems.Should().HaveCount(2);
        _sut.LineItems.Should().Contain(x => x.Name == "Toplam Gelir");
        _sut.LineItems.Should().Contain(x => x.Name == "Toplam Gider");
        _sut.LineItems.Should().Contain(x => x.Type == "Gelir");
        _sut.LineItems.Should().Contain(x => x.Type == "Gider");
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateKPIs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — revenue=125480, expenses=87320, profit=38160, margin~30.4%
        _sut.TotalRevenue.Should().Contain("125");
        _sut.TotalExpenses.Should().Contain("87");
        _sut.NetProfit.Should().Contain("38");
        _sut.ProfitMarginText.Should().Contain("%");
        _sut.ProfitMarginText.Should().Contain("30");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(KarZararAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true, "IsLoading should transition to true during load");
        _sut.IsLoading.Should().BeFalse("IsLoading should be false after load completes");
    }

    [Fact]
    public async Task PrevMonth_ShouldDecrementPeriod()
    {
        // Arrange
        var initialLabel = _sut.PeriodLabel;

        // Act
        await _sut.PrevMonthCommand.ExecuteAsync(null);

        // Assert — period label should change
        _sut.PeriodLabel.Should().NotBe(initialLabel);
        // After going back one month, items should still load
        _sut.LineItems.Should().HaveCount(2);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task NextMonth_ShouldIncrementPeriod()
    {
        // Arrange — go back first so we can go forward
        await _sut.PrevMonthCommand.ExecuteAsync(null);
        var afterPrevLabel = _sut.PeriodLabel;

        // Act
        await _sut.NextMonthCommand.ExecuteAsync(null);

        // Assert — should return to original period
        _sut.PeriodLabel.Should().NotBe(afterPrevLabel);
        _sut.LineItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task PrevMonth_ThenNextMonth_ReturnsToOriginal()
    {
        // Arrange
        var originalLabel = _sut.PeriodLabel;

        // Act
        await _sut.PrevMonthCommand.ExecuteAsync(null);
        await _sut.NextMonthCommand.ExecuteAsync(null);

        // Assert
        _sut.PeriodLabel.Should().Be(originalLabel);
    }

    [Fact]
    public async Task IsEmpty_ShouldBeFalseWhenItemsExist()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsEmpty.Should().BeFalse();
        _sut.LineItems.Should().NotBeEmpty();
    }
}
