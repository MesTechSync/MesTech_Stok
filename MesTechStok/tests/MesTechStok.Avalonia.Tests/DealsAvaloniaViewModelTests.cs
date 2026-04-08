using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Avalonia.Services;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class DealsAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private DealsAvaloniaViewModel CreateSut()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDealsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetDealsResult { Items = [], TotalCount = 0 });
        return new DealsAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>(), Mock.Of<IDialogService>());
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
    public async Task LoadAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenEmpty_ShouldSetEmptyState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsEmpty.Should().BeTrue();
        sut.Deals.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
        sut.TotalAmount.Should().Be("0 TL");
    }

    [Fact]
    public void DealItemVm_ShouldFormatDisplayProperties()
    {
        // Assert — verify formatting via DTO directly
        var deal = new DealListItemVm
        {
            Title = "Test Projesi",
            Amount = 45000m,
            Probability = 30,
            ContactName = "Ahmet Yilmaz"
        };
        deal.AmountDisplay.Should().Be("45.000 TL");
        deal.ProbabilityDisplay.Should().Be("%30");
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
    }
}
