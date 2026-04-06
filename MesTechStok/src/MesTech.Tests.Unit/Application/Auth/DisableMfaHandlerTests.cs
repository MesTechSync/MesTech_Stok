using FluentAssertions;
using MesTech.Application.Features.Auth.Commands.DisableMfa;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Auth;

// ════════════════════════════════════════════════════════
// DEV5 TUR 9: DisableMfa handler tests — security critical
// OWASP ASVS V2.8 multi-factor: disable requires verification
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Security", "MFA")]
public class DisableMfaHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITotpService> _totpService = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<DisableMfaHandler>> _logger = new();

    private DisableMfaHandler CreateSut() =>
        new(_userRepo.Object, _totpService.Object, _uow.Object, _logger.Object);

    private static User CreateMfaEnabledUser(Guid userId)
    {
        var user = new User
        {
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = "hashed",
            TenantId = Guid.NewGuid()
        };
        // Protected set — use reflection for test
        typeof(MesTech.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(user, userId);
        user.IsMfaEnabled = true;
        user.TotpSecret = "JBSWY3DPEHPK3PXP";
        return user;
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnError()
    {
        var sut = CreateSut();
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await sut.Handle(
            new DisableMfaCommand(Guid.NewGuid(), "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamadi");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MfaAlreadyDisabled_ShouldReturnError()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var user = CreateMfaEnabledUser(userId);
        user.IsMfaEnabled = false;
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await sut.Handle(
            new DisableMfaCommand(userId, "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("zaten deaktif");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidTotpCode_ShouldReturnError()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var user = CreateMfaEnabledUser(userId);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpService.Setup(s => s.VerifyCode(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var result = await sut.Handle(
            new DisableMfaCommand(userId, "000000"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Gecersiz dogrulama kodu");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCode_ShouldDisableMfaAndClearSecret()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var user = CreateMfaEnabledUser(userId);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpService.Setup(s => s.VerifyCode("JBSWY3DPEHPK3PXP", "123456")).Returns(true);

        var result = await sut.Handle(
            new DisableMfaCommand(userId, "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        user.IsMfaEnabled.Should().BeFalse();
        user.TotpSecret.Should().BeNull();
        _userRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyTotpSecret_ShouldReturnError()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var user = CreateMfaEnabledUser(userId);
        user.TotpSecret = null;
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await sut.Handle(
            new DisableMfaCommand(userId, "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Gecersiz dogrulama kodu");
    }
}
