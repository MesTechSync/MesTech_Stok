using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class DealsAvaloniaViewModelTests
{
    private static DealsAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        return new DealsAvaloniaViewModel(mediatorMock.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.SearchText.Should().BeEmpty();
        sut.SelectedStage.Should().BeNull();
        sut.TotalCount.Should().Be(0);
        sut.TotalAmount.Should().Be("0 TL");
        sut.Deals.Should().BeEmpty();
        sut.StageOptions.Should().HaveCount(6);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateDeals()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Deals.Should().HaveCount(4);
        sut.TotalCount.Should().Be(4);
        sut.TotalAmount.Should().NotBe("0 TL");
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateTotalAmount()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — 45000 + 22000 + 67000 + 35000 = 169000
        sut.TotalAmount.Should().Be("169.000 TL");
        sut.Deals.Should().Contain(d => d.Stage == "Kazanildi" && d.Probability == 100);
    }

    [Fact]
    public async Task LoadAsync_DealItemVm_ShouldFormatDisplayProperties()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        var deal = sut.Deals.First(d => d.Title == "ABC Ltd ERP Projesi");
        deal.AmountDisplay.Should().Be("45.000 TL");
        deal.ProbabilityDisplay.Should().Be("%30");
        deal.ContactName.Should().Be("Ahmet Yilmaz");
    }

    [Fact]
    public async Task RefreshCommand_ShouldDelegateToLoadAsync()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.RefreshCommand.ExecuteAsync(null);

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.Deals.Should().HaveCount(4);
        sut.TotalCount.Should().Be(4);
    }
}
