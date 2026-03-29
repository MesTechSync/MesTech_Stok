using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class SupplierAvaloniaViewModelTests
{
    private static SupplierAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        return new SupplierAvaloniaViewModel(mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    // ── 3-State: Default ──

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
        sut.Suppliers.Should().BeEmpty();
    }

    // ── 3-State: Loading → Loaded ──

    [Fact]
    public async Task LoadAsync_ShouldPopulateSuppliersAndSetCounts()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Suppliers.Should().HaveCount(3);
        sut.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task LoadAsync_SupplierData_ShouldContainExpectedFields()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        var first = sut.Suppliers[0];
        first.SupplierName.Should().Be("ABC Elektronik Ltd.");
        first.ContactPerson.Should().Be("Hasan Yildiz");
        first.Email.Should().Contain("@");
        first.City.Should().Be("Istanbul");
        first.Balance.Should().Be(125000.00m);
    }

    // ── 3-State: Refresh ──

    [Fact]
    public async Task RefreshCommand_ShouldReloadSuppliers()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        await sut.RefreshCommand.ExecuteAsync(null);

        // Assert
        sut.Suppliers.Should().HaveCount(3);
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_MultipleCalls_ShouldNotDuplicateData()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();
        await sut.LoadAsync();

        // Assert — collection should be cleared and reloaded, not appended
        sut.Suppliers.Should().HaveCount(3);
        sut.TotalCount.Should().Be(3);
    }
}
