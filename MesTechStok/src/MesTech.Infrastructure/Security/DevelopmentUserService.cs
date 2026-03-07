using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// Development ortamı için ICurrentUserService implementasyonu.
/// Login sistemi olmadığı sürece varsayılan kullanıcı döner.
/// </summary>
public class DevelopmentUserService : ICurrentUserService
{
    public int? UserId => 1;
    public string? Username => "dev-user";
    public bool IsAuthenticated => true;
}
