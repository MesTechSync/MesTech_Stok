using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ContactAvaloniaViewModelTests
{
    private static ContactAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        var tenantMock = new Mock<ITenantProvider>();
        return new ContactAvaloniaViewModel(mediatorMock.Object, tenantMock.Object);
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
        sut.Contacts.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateContacts()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Contacts.Should().HaveCount(4);
        sut.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task LoadAsync_ShouldContainExpectedContactData()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Contacts.Should().Contain(c => c.FullName == "Ahmet Yilmaz" && c.Company == "ABC Ltd");
        sut.Contacts.Should().Contain(c => c.Type == "Tedarikci");
        sut.Contacts.Should().Contain(c => c.City == "Ankara");
    }

    [Fact]
    public async Task LoadAsync_MultipleCalls_ShouldClearAndReload()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();
        await sut.LoadAsync();

        // Assert — should not double-add, collection cleared each time
        sut.Contacts.Should().HaveCount(4);
        sut.TotalCount.Should().Be(4);
        sut.HasError.Should().BeFalse();
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
        sut.Contacts.Should().HaveCount(4);
        sut.TotalCount.Should().Be(4);
    }
}
