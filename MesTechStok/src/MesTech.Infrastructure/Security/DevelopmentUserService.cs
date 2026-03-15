using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// Development ortamı için ICurrentUserService implementasyonu.
/// Login sistemi olmadığı sürece varsayılan kullanıcı döner.
/// </summary>
public class DevelopmentUserService : ICurrentUserService
{
    public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
    public Guid TenantId => Guid.Parse("00000000-0000-0000-0000-000000000001");
    public string? Username => "dev-user";
    public bool IsAuthenticated => true;
}
