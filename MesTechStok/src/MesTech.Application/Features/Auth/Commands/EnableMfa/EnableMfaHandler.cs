using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Auth.Commands.EnableMfa;

public sealed class EnableMfaHandler : IRequestHandler<EnableMfaCommand, EnableMfaResult>
{
    private readonly IUserRepository _userRepo;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<EnableMfaHandler> _logger;

    public EnableMfaHandler(
        IUserRepository userRepo,
        ITotpService totpService,
        IUnitOfWork uow,
        ILogger<EnableMfaHandler> logger)
    {
        _userRepo = userRepo;
        _totpService = totpService;
        _uow = uow;
        _logger = logger;
    }

    public async Task<EnableMfaResult> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return new EnableMfaResult { ErrorMessage = "Kullanici bulunamadi" };

        if (user.IsMfaEnabled)
            return new EnableMfaResult { ErrorMessage = "MFA zaten aktif" };

        var secret = _totpService.GenerateSecret();
        var qrUri = _totpService.GenerateQrCodeUri(secret, user.Email ?? user.Username);

        user.TotpSecret = secret;
        await _userRepo.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("MFA setup baslatildi: UserId={UserId}", request.UserId);

        return new EnableMfaResult
        {
            IsSuccess = true,
            Secret = secret,
            QrCodeUri = qrUri
        };
    }
}
