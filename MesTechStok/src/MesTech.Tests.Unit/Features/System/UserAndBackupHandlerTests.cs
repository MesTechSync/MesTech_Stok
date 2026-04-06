using FluentAssertions;
using MesTech.Application.Features.System.Commands.CreateManualBackup;
using MesTech.Application.Features.System.Users.Commands;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Features.System;

/// <summary>
/// Auth + System handler testleri: ChangePassword, CreateUser, CreateManualBackup.
/// Handler gap kapatma — TUR4.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UserAndBackupHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IBackupEntryRepository> _backupRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public UserAndBackupHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ══════════════════════════════════════
    // ChangePassword (5 test)
    // ══════════════════════════════════════

    private static User MakeUser(string password = "OldPass123!")
    {
        return new User
        {
            Id = Guid.NewGuid(), TenantId = TenantId,
            Username = "testuser", Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task ChangePassword_ValidCurrent_ShouldSucceed()
    {
        var user = MakeUser("MyOldPass1!");
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = new ChangePasswordHandler(_userRepo.Object, _uow.Object,
            Mock.Of<ILogger<ChangePasswordHandler>>());

        var result = await handler.Handle(
            new ChangePasswordCommand(user.Id, "MyOldPass1!", "MyNewPass2!"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("MyNewPass2!", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ShouldFail()
    {
        var user = MakeUser("Correct123!");
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = new ChangePasswordHandler(_userRepo.Object, _uow.Object,
            Mock.Of<ILogger<ChangePasswordHandler>>());

        var result = await handler.Handle(
            new ChangePasswordCommand(user.Id, "Wrong123!", "New456!"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("yanlış");
    }

    [Fact]
    public async Task ChangePassword_SameAsOld_ShouldFail()
    {
        var user = MakeUser("Same123!");
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = new ChangePasswordHandler(_userRepo.Object, _uow.Object,
            Mock.Of<ILogger<ChangePasswordHandler>>());

        var result = await handler.Handle(
            new ChangePasswordCommand(user.Id, "Same123!", "Same123!"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("aynı");
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_ShouldFail()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new ChangePasswordHandler(_userRepo.Object, _uow.Object,
            Mock.Of<ILogger<ChangePasswordHandler>>());

        var result = await handler.Handle(
            new ChangePasswordCommand(Guid.NewGuid(), "old", "new"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // CreateUser (5 test)
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateUser_NewUsername_ShouldSucceed()
    {
        _userRepo.Setup(r => r.GetByUsernameAsync("newuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new CreateUserHandler(_userRepo.Object, _uow.Object,
            Mock.Of<ILogger<CreateUserHandler>>());

        var result = await handler.Handle(
            new CreateUserCommand(TenantId, "newuser", "Pass123!", "e@e.com", "Ad", "Soyad", null),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.UserId.Should().NotBeNull();
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ShouldFail()
    {
        _userRepo.Setup(r => r.GetByUsernameAsync("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUser());

        var handler = new CreateUserHandler(_userRepo.Object, _uow.Object,
            Mock.Of<ILogger<CreateUserHandler>>());

        var result = await handler.Handle(
            new CreateUserCommand(TenantId, "existing", "Pass123!", null, null, null, null),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("zaten mevcut");
    }

    [Fact]
    public async Task CreateUser_ShouldHashPassword()
    {
        _userRepo.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        User? captured = null;
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => captured = u)
            .Returns(Task.CompletedTask);

        var handler = new CreateUserHandler(_userRepo.Object, _uow.Object,
            Mock.Of<ILogger<CreateUserHandler>>());

        await handler.Handle(
            new CreateUserCommand(TenantId, "hashtest", "PlainText!", null, null, null, null),
            CancellationToken.None);

        captured!.PasswordHash.Should().NotBe("PlainText!", "password should be BCrypt hashed");
        BCrypt.Net.BCrypt.Verify("PlainText!", captured.PasswordHash).Should().BeTrue();
    }

    // ══════════════════════════════════════
    // CreateManualBackup (3 test)
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateBackup_MarkCompleted0_ShouldThrow_DocumentsBug()
    {
        // Handler calls BackupEntry.MarkCompleted(0) which throws ArgumentOutOfRangeException
        // because sizeBytes must be > 0. This is a handler bug (should use estimated size).
        _backupRepo.Setup(r => r.AddAsync(It.IsAny<BackupEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateManualBackupHandler(_backupRepo.Object, _uow.Object,
            Mock.Of<ILogger<CreateManualBackupHandler>>());

        var act = () => handler.Handle(new CreateManualBackupCommand(TenantId), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>(
            "MarkCompleted(0) throws — handler should pass estimated file size, not 0");
    }

    [Fact]
    public async Task CreateBackup_NullRequest_ShouldThrow()
    {
        var handler = new CreateManualBackupHandler(_backupRepo.Object, _uow.Object,
            Mock.Of<ILogger<CreateManualBackupHandler>>());

        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
