using FluentAssertions;
using MesTech.WebApi.Hubs;
using System.Reflection;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// G104 REGRESYON: MesTechHub.JoinTenantGroup(tenantId) client'ın gönderdiği
/// herhangi bir tenantId'yi kabul ediyor — JWT tenant_id claim doğrulaması YOK.
///
/// Bu test hub metodunun parametresini kontrol eder.
/// Bug düzeltildiğinde hub metodu Context.User tenant claim'i doğrulayacak.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Bug", "G104")]
public class SignalRTenantBypassRegressionTests
{
    [Fact(DisplayName = "G104: JoinTenantGroup accepts string tenantId — no JWT validation")]
    public void G104_JoinTenantGroup_AcceptsAnyTenantId()
    {
        // JoinTenantGroup metodu "string tenantId" parametre alıyor
        // Bu parametreyi JWT claim ile karşılaştırmıyor
        var hubType = typeof(MesTechHub);
        var method = hubType.GetMethod("JoinTenantGroup");

        method.Should().NotBeNull("JoinTenantGroup metodu mevcut olmalı");

        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(1, "Metod tek parametre almalı");
        parameters[0].ParameterType.Should().Be(typeof(string),
            "G104: tenantId string olarak client'tan alınıyor — " +
            "JWT claim doğrulaması eklenene kadar cross-tenant leak riski");

        // G104 FIX SONRASI: Bu test güncellenecek
        // Metod ya tenantId parametresini kaldırıp Context.User'dan alacak
        // Ya da internal'da JWT claim karşılaştırması yapacak
    }

    [Fact(DisplayName = "G104: Hub has [Authorize] but no tenant-specific authorization")]
    public void G104_Hub_HasAuthorize_ButNoTenantAuth()
    {
        var hubType = typeof(MesTechHub);
        var authorizeAttrs = hubType.GetCustomAttributes()
            .Where(a => a.GetType().Name.Contains("Authorize"))
            .ToList();

        authorizeAttrs.Should().NotBeEmpty("[Authorize] attribute mevcut");

        // Ama Roles veya Policy ile tenant kısıtlaması YOK
        var hasRoleOrPolicy = authorizeAttrs.Any(a =>
        {
            var rolesProperty = a.GetType().GetProperty("Roles");
            var policyProperty = a.GetType().GetProperty("Policy");
            var roles = rolesProperty?.GetValue(a) as string;
            var policy = policyProperty?.GetValue(a) as string;
            return !string.IsNullOrEmpty(roles) || !string.IsNullOrEmpty(policy);
        });

        hasRoleOrPolicy.Should().BeFalse(
            "G104: Hub [Authorize] herhangi bir authenticated user'ı kabul eder — " +
            "tenant-specific policy YOK. JoinTenantGroup ile başka tenant'a katılabilir.");
    }
}
