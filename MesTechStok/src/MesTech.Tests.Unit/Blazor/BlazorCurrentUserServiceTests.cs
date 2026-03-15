using FluentAssertions;
using MesTech.Domain.Interfaces;

namespace MesTech.Tests.Unit.Blazor;

/// <summary>
/// BlazorCurrentUserService unit tests — Dalga 9 PoC.
/// DEV 2 Blazor projesi olusturuldugunda compile olacak.
/// Su an ICurrentUserService interface'i uzerinden PoC pattern dogrulama.
///
/// BlazorCurrentUserService expected location:
///   MesTech.Blazor/Services/BlazorCurrentUserService.cs
/// Expected behavior: PoC modunda sabit tenant, authenticated=true, default username.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BlazorPoC")]
public class BlazorCurrentUserServiceTests
{
    // PoC default tenant — tum MesTech modullerinde ortak
    private static readonly Guid DefaultTenantId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// BlazorCurrentUserService PoC pattern:
    /// ICurrentUserService implementasyonunda TenantId sabit default GUID olmali.
    /// Blazor projesi hazir olana kadar bu testi interface mock ile dogruluyoruz.
    /// </summary>
    [Fact]
    public void TenantId_ShouldBeDefaultTenantGuid()
    {
        // Arrange — PoC BlazorCurrentUserService davranisini simule et
        var sut = new PocBlazorCurrentUserService();

        // Act
        var tenantId = sut.TenantId;

        // Assert
        tenantId.Should().Be(DefaultTenantId,
            "PoC modunda BlazorCurrentUserService sabit default tenant donmeli");
        tenantId.Should().NotBe(Guid.Empty,
            "TenantId bos GUID olmamali — global query filter bozulur");
    }

    /// <summary>
    /// PoC modunda IsAuthenticated her zaman true olmali.
    /// Login mekanizmasi Dalga 12'de eklenecek — su an bypass.
    /// </summary>
    [Fact]
    public void IsAuthenticated_ShouldBeTrue_InPocMode()
    {
        // Arrange
        var sut = new PocBlazorCurrentUserService();

        // Act
        var isAuthenticated = sut.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeTrue(
            "PoC modunda authentication bypass aktif — IsAuthenticated=true olmali");
    }

    /// <summary>
    /// Username bos olmamali — loglama ve audit trail icin gerekli.
    /// PoC modunda "blazor-poc-user" veya benzeri sabit deger beklenir.
    /// </summary>
    [Fact]
    public void UserName_ShouldNotBeEmpty()
    {
        // Arrange
        var sut = new PocBlazorCurrentUserService();

        // Act
        var username = sut.Username;

        // Assert
        username.Should().NotBeNullOrWhiteSpace(
            "Username bos olmamali — audit log ve loglama icin gerekli");
    }

    // ══════════════════════════════════════════════════════════════
    //  PoC stub — DEV 2 gercek BlazorCurrentUserService olusturunca
    //  bu stub kaldirilip gercek sinif referans edilecek.
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Temporary PoC stub implementing ICurrentUserService.
    /// Mirrors the expected BlazorCurrentUserService behavior.
    /// Will be replaced when MesTech.Blazor project is created by DEV 2.
    /// </summary>
    private sealed class PocBlazorCurrentUserService : ICurrentUserService
    {
        private static readonly Guid _defaultTenantId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        public Guid? UserId => Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
        public Guid TenantId => _defaultTenantId;
        public string? Username => "blazor-poc-user";
        public bool IsAuthenticated => true;
    }
}
