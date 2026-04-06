using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetContactsPaged;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ContactAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private ContactAvaloniaViewModel CreateSut()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetContactsPagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContactsPagedResult { Contacts = [], TotalCount = 0 });
        var tenantMock = new Mock<ITenantProvider>();
        return new ContactAvaloniaViewModel(_mediatorMock.Object, tenantMock.Object, Mock.Of<MesTech.Avalonia.Services.IDialogService>());
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
    public async Task LoadAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetEmptyWhenNoContacts()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Contacts.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_MultipleCalls_ShouldNotDoubleAdd()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();
        await sut.LoadAsync();

        // Assert — should not double-add, collection cleared each time
        sut.Contacts.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
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
        sut.HasError.Should().BeFalse();
    }
}
