using FluentAssertions;
using MesTech.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// G072 REGRESYON: ApiTenantProvider null HttpContext → Guid.Empty döndürüyor.
/// Multi-tenant uygulamada Guid.Empty tenant isolation bypass'a yol açar.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Bug", "G072")]
public class ApiTenantProviderRegressionTests
{
    [Fact(DisplayName = "G072: Null HttpContext returns Guid.Empty — tenant isolation bypass risk")]
    public void G072_NullHttpContext_ReturnsGuidEmpty()
    {
        // ARRANGE: HttpContext null olduğunda (background job, migration)
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var sut = new ApiTenantProvider(accessor.Object);

        // ACT
        var tenantId = sut.GetCurrentTenantId();

        // ASSERT: Bug kanıtı — Guid.Empty dönüyor
        tenantId.Should().Be(Guid.Empty,
            "G072: ApiTenantProvider returns Guid.Empty when HttpContext is null — " +
            "this SHOULD throw InvalidOperationException instead");

        // Bu test, fix uygulandığında Should().Throw<InvalidOperationException>() olarak güncellenmelidir
    }

    [Fact(DisplayName = "G072: Guid.Empty matches Guid.Empty in EF global filter — data leak")]
    public void G072_GuidEmpty_MatchesGuidEmpty_InFilter()
    {
        // RLS/Global query filter: WHERE TenantId = @currentTenantId
        // Eğer currentTenantId = Guid.Empty VE DB'de TenantId = Guid.Empty olan satır varsa
        // → cross-tenant veri sızıntısı

        var filterTenantId = Guid.Empty; // ApiTenantProvider'dan gelen
        var dbTenantId = Guid.Empty;     // Yanlışlıkla DB'ye yazılmış Guid.Empty

        (filterTenantId == dbTenantId).Should().BeTrue(
            "G072: Guid.Empty == Guid.Empty is true — RLS filter doesn't protect against it");
    }
}
