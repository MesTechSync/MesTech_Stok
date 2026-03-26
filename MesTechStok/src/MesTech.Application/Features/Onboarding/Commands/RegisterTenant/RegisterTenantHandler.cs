using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Onboarding.Commands.RegisterTenant;

public sealed class RegisterTenantHandler
    : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
{
    private readonly ITenantRepository _tenantRepo;
    private readonly IUserRepository _userRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly ITenantSubscriptionRepository _subscriptionRepo;
    private readonly IOnboardingProgressRepository _onboardingRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RegisterTenantHandler> _logger;

    public RegisterTenantHandler(
        ITenantRepository tenantRepo,
        IUserRepository userRepo,
        ISubscriptionPlanRepository planRepo,
        ITenantSubscriptionRepository subscriptionRepo,
        IOnboardingProgressRepository onboardingRepo,
        IUnitOfWork uow,
        ILogger<RegisterTenantHandler> logger)
    {
        _tenantRepo = tenantRepo;
        _userRepo = userRepo;
        _planRepo = planRepo;
        _subscriptionRepo = subscriptionRepo;
        _onboardingRepo = onboardingRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<RegisterTenantResult> Handle(
        RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Yeni tenant kayıt: {Company}, Admin: {Username}",
            request.CompanyName, request.AdminUsername);

        // 1. Kullanıcı adı benzersizliği kontrol
        var existingUser = await _userRepo.GetByUsernameAsync(request.AdminUsername)
            .ConfigureAwait(false);
        if (existingUser is not null)
            throw new InvalidOperationException($"Kullanıcı adı zaten kullanılıyor: {request.AdminUsername}");

        // 2. Tenant oluştur
        var tenant = new Domain.Entities.Tenant
        {
            Name = request.CompanyName,
            TaxNumber = request.TaxNumber,
            IsActive = true
        };
        await _tenantRepo.AddAsync(tenant, cancellationToken).ConfigureAwait(false);

        // 3. Admin kullanıcı oluştur (BCrypt hash)
        // FIX-DEV6-ÖZ-DENETİM: TenantId atanmıyordu → user hangi tenant'a ait bilinmiyordu
        var adminUser = new User
        {
            TenantId = tenant.Id,
            Username = request.AdminUsername,
            Email = request.AdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword),
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName,
            IsActive = true
        };
        await _userRepo.AddAsync(adminUser).ConfigureAwait(false);

        // 4. Trial subscription başlat (en düşük plan, 14 gün)
        var plans = await _planRepo.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        var starterPlan = plans
            .OrderBy(p => p.MonthlyPrice)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Aktif plan bulunamadı — seed data eksik.");

        var subscription = TenantSubscription.StartTrial(
            tenant.Id, starterPlan.Id, starterPlan.TrialDays);
        await _subscriptionRepo.AddAsync(subscription, cancellationToken).ConfigureAwait(false);

        // 5. Onboarding progress başlat
        var onboarding = OnboardingProgress.Start(tenant.Id);
        await _onboardingRepo.AddAsync(onboarding, cancellationToken).ConfigureAwait(false);

        // 6. Atomik kaydet
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Tenant kayıt tamamlandı: TenantId={TenantId}, UserId={UserId}, SubscriptionId={SubId}, Trial={TrialDays}d",
            tenant.Id, adminUser.Id, subscription.Id, starterPlan.TrialDays);

        return new RegisterTenantResult
        {
            TenantId = tenant.Id,
            AdminUserId = adminUser.Id,
            SubscriptionId = subscription.Id,
            OnboardingId = onboarding.Id,
            TrialEndsAt = subscription.TrialEndsAt ?? DateTime.UtcNow.AddDays(14),
            PlanName = starterPlan.Name
        };
    }
}
