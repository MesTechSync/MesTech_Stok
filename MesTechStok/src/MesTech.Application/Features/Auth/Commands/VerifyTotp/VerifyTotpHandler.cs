using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Auth.Commands.VerifyTotp;

public sealed class VerifyTotpHandler : IRequestHandler<VerifyTotpCommand, VerifyTotpResult>
{
    private readonly IUserRepository _userRepo;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<VerifyTotpHandler> _logger;

    public VerifyTotpHandler(
        IUserRepository userRepo,
        ITotpService totpService,
        IUnitOfWork uow,
        ILogger<VerifyTotpHandler> logger)
    {
        _userRepo = userRepo;
        _totpService = totpService;
        _uow = uow;
        _logger = logger;
    }

    public async Task<VerifyTotpResult> Handle(VerifyTotpCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(request.UserId);
        if (user is null)
            return new VerifyTotpResult { ErrorMessage = "Kullanici bulunamadi" };

        if (string.IsNullOrEmpty(user.TotpSecret))
            return new VerifyTotpResult { ErrorMessage = "MFA henuz kurulmamis — once EnableMfa cagirin" };

        var isValid = _totpService.VerifyCode(user.TotpSecret, request.Code);
        if (!isValid)
        {
            _logger.LogWarning("MFA dogrulama basarisiz: UserId={UserId}", request.UserId);
            return new VerifyTotpResult { ErrorMessage = "Gecersiz kod" };
        }

        if (!user.IsMfaEnabled)
        {
            user.IsMfaEnabled = true;
            user.MfaEnabledAt = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);
            await _uow.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("MFA aktif edildi: UserId={UserId}", request.UserId);
        }

        return new VerifyTotpResult { IsSuccess = true };
    }
}
