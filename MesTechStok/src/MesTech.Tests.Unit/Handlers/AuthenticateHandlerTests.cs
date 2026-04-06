using FluentAssertions;
using MesTech.Application.Features.Auth.Commands.Authenticate;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
[Trait("Layer", "Auth")]
public class AuthenticateHandlerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ILogger<AuthenticateHandler>> _logger = new();

    private AuthenticateHandler CreateSut() => new(_authService.Object, _userRepo.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnSuccess()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _authService.Setup(a => a.ValidateAsync("admin", "pass123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResult.Success(userId, tenantId, "Admin User"));

        var sut = CreateSut();
        var result = await sut.Handle(new AuthenticateCommand("admin", "pass123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.UserId.Should().Be(userId);
        result.UserName.Should().Be("Admin User");
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_InvalidCredentials_ShouldReturnFailure()
    {
        _authService.Setup(a => a.ValidateAsync("admin", "wrong", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResult.Fail("Gecersiz kullanici adi veya sifre."));

        var sut = CreateSut();
        var result = await sut.Handle(new AuthenticateCommand("admin", "wrong"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Gecersiz");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();
        var act = async () => await sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_AuthServiceThrows_ShouldReturnFailure()
    {
        _authService.Setup(a => a.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB baglanti hatasi"));

        var sut = CreateSut();
        var result = await sut.Handle(new AuthenticateCommand("admin", "pass"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_ShouldCallUserRepo()
    {
        var userId = Guid.NewGuid();
        _authService.Setup(a => a.ValidateAsync("test", "pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResult.Success(userId, Guid.NewGuid(), "Test User"));

        var sut = CreateSut();
        await sut.Handle(new AuthenticateCommand("test", "pass"), CancellationToken.None);

        _userRepo.Verify(r => r.GetByUsernameAsync("test", It.IsAny<CancellationToken>()), Times.Once);
    }
}
