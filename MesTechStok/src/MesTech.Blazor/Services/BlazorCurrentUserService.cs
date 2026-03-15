using MesTech.Domain.Interfaces;

namespace MesTech.Blazor.Services;

/// <summary>
/// Blazor Server PoC icin ICurrentUserService implementasyonu.
/// Dalga 10+ authentication eklenince Blazor auth state ile degistirilecek.
/// </summary>
public class BlazorCurrentUserService : ICurrentUserService
{
    public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000002");
    public Guid TenantId => Guid.Parse("00000000-0000-0000-0000-000000000001");
    public string? Username => "Demo Kullanici";
    public bool IsAuthenticated => true;
}
