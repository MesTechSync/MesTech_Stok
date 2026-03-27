using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CustomerAvaloniaViewModelTests
{
    private static CustomerAvaloniaViewModel CreateSut()
    {
        return new CustomerAvaloniaViewModel(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>());
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
        sut.TotalCount.Should().Be(0);
        sut.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateItems()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Items.Should().HaveCount(10);
        sut.TotalCount.Should().Be(10);
    }

    [Fact]
    public async Task LoadAsync_ShouldContainExpectedCustomerData()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Items.Should().Contain(c => c.AdSoyad == "Ahmet Yilmaz" && c.Sehir == "Istanbul");
        sut.Items.Should().Contain(c => c.SiparisSayisi == 42);
        sut.Items.Should().Contain(c => c.Email == "elif.yildiz@outlook.com");
    }

    [Fact]
    public async Task SearchText_WhenSet_ShouldFilterItems()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SearchText = "Ahmet";

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items[0].AdSoyad.Should().Be("Ahmet Yilmaz");
        sut.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task LoadAsync_MultipleCalls_ShouldClearAndReload()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();
        await sut.LoadAsync();

        // Assert — should not double-add
        sut.Items.Should().HaveCount(10);
        sut.TotalCount.Should().Be(10);
        sut.HasError.Should().BeFalse();
    }
}
