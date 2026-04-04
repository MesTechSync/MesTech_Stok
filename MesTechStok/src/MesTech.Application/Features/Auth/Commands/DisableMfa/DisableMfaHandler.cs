using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Auth.Commands.DisableMfa;

public sealed class DisableMfaHandler : IRequestHandler<DisableMfaCommand, DisableMfaResult>
{
    private readonly IUserRepository _userRepo;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DisableMfaHandler> _logger;

    public DisableMfaHandler(
        IUserRepository userRepo,
        ITotpService totpService,
        IUnitOfWork uow,
        ILogger<DisableMfaHandler> logger)
    {
        _userRepo = userRepo;
        _totpService = totpService;
        _uow = uow;
        _logger = logger;
    }

    public async Task<DisableMfaResult> Handle(DisableMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return new DisableMfaResult { ErrorMessage = "Kullanici bulunamadi" };

        if (!user.IsMfaEnabled)
            return new DisableMfaResult { ErrorMessage = "MFA zaten deaktif" };

        // Verify TOTP code before disabling (security: prevent unauthorized disable)
        if (string.IsNullOrWhiteSpace(user.TotpSecret) ||
            !_totpService.VerifyCode(user.TotpSecret, request.TotpCode))
        {
            return new DisableMfaResult { ErrorMessage = "Gecersiz dogrulama kodu" };
        }

        user.IsMfaEnabled = false;
        user.TotpSecret = null;
        await _userRepo.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("MFA devre disi birakildi: UserId={UserId}", request.UserId);

        return new DisableMfaResult { IsSuccess = true };
    }
}
