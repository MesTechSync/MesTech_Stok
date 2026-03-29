using FluentAssertions;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class LoginAvaloniaViewModelTests
{
    private readonly LoginAvaloniaViewModel _sut;

    public LoginAvaloniaViewModelTests()
    {
        _sut = new LoginAvaloniaViewModel(Mock.Of<IMediator>(), Mock.Of<IAuthService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.Username.Should().BeEmpty();
        _sut.Password.Should().BeEmpty();
        _sut.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldComplete()
    {
        await _sut.LoadAsync();

        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_EmptyCredentials_ShouldSetError()
    {
        _sut.Username = "";
        _sut.Password = "";

        await _sut.LoginCommand.ExecuteAsync(null);

        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Contain("bos");
        _sut.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_EmptyPassword_ShouldSetError()
    {
        _sut.Username = "admin";
        _sut.Password = "";

        await _sut.LoginCommand.ExecuteAsync(null);

        _sut.HasError.Should().BeTrue();
        _sut.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_ValidCredentials_ShouldAuthenticate()
    {
        _sut.Username = "admin";
        _sut.Password = "password123";

        await _sut.LoginCommand.ExecuteAsync(null);

        _sut.IsAuthenticated.Should().BeTrue();
        _sut.HasError.Should().BeFalse();
        _sut.WelcomeMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PropertyBinding_ShouldRaisePropertyChanged()
    {
        var changedProperties = new List<string>();
        _sut.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        _sut.Username = "testuser";
        _sut.Password = "testpass";

        changedProperties.Should().Contain("Username");
        changedProperties.Should().Contain("Password");
    }
}
