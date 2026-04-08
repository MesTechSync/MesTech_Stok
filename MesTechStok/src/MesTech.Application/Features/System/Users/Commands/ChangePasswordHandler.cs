using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.System.Users.Commands;

public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordResult>
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(
        IUserRepository userRepo,
        IUnitOfWork uow,
        ILogger<ChangePasswordHandler> logger)
    {
        _userRepo = userRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<ChangePasswordResult> Handle(
        ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.LogWarning("ChangePassword failed: user {UserId} not found", request.UserId);
            return ChangePasswordResult.Fail("Kullanıcı bulunamadı.");
        }

        // Mevcut şifre doğrula
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("ChangePassword failed: wrong current password for user {UserId}", request.UserId);
            return ChangePasswordResult.Fail("Mevcut şifre yanlış.");
        }

        // Yeni şifre eski ile aynı olmamalı
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
        {
            return ChangePasswordResult.Fail("Yeni şifre eski şifre ile aynı olamaz.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Password changed for user {UserId} ({Username})", user.Id, user.Username);
        return ChangePasswordResult.Ok();
    }
}
