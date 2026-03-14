using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CreateProject;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Events.Crm;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Infrastructure.Persistence.Repositories.Crm;
using NetArchTest.Rules;

namespace MesTech.Tests.Architecture;

[Trait("Category", "Architecture")]
public class CrmLayerDependencyTests
{
    // ── 1. CRM entities must not reference Infrastructure ──────────────────

    [Fact]
    public void CrmEntities_ShouldNotReferenceInfrastructure()
    {
        var result = Types.InAssembly(typeof(Lead).Assembly)
            .That()
            .ResideInNamespace("MesTech.Domain.Entities.Crm")
            .ShouldNot()
            .HaveDependencyOn("MesTech.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "CRM domain entities must not depend on infrastructure.");
    }

    // ── 2. CRM entities must not reference Application ─────────────────────

    [Fact]
    public void CrmEntities_ShouldNotReferenceApplication()
    {
        var result = Types.InAssembly(typeof(Lead).Assembly)
            .That()
            .ResideInNamespace("MesTech.Domain.Entities.Crm")
            .ShouldNot()
            .HaveDependencyOn("MesTech.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "CRM domain entities must not depend on application layer.");
    }

    // ── 3. Task entities must not reference outer layers ───────────────────

    [Fact]
    public void TaskEntities_ShouldNotReferenceOuterLayers()
    {
        var result = Types.InAssembly(typeof(WorkTask).Assembly)
            .That()
            .ResideInNamespace("MesTech.Domain.Entities.Tasks")
            .ShouldNot()
            .HaveDependencyOnAny("MesTech.Application", "MesTech.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Task domain entities must not depend on Application or Infrastructure.");
    }

    // ── 4. Task handlers must only reference Domain + Application ──────────

    [Fact]
    public void TaskHandlers_ShouldOnlyDependOnDomainAndApplication()
    {
        var result = Types.InAssembly(typeof(CreateWorkTaskHandler).Assembly)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .ResideInNamespace("MesTech.Application.Features.Tasks")
            .ShouldNot()
            .HaveDependencyOn("MesTech.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Task handlers in Application layer must not depend on Infrastructure.");
    }

    // ── 5. Platform adapters must implement IIntegratorAdapter ────────────
    // Cargo adapters (Yurtici/Aras/Surat) implement ICargoAdapter — not checked here.

    [Fact]
    public void PlatformAdapters_ShouldImplementIIntegratorAdapter()
    {
        // Verify a representative set of known platform adapters implement IIntegratorAdapter.
        typeof(TrendyolAdapter).Should().Implement<IIntegratorAdapter>(
            "TrendyolAdapter must implement IIntegratorAdapter");
        typeof(EbayAdapter).Should().Implement<IIntegratorAdapter>(
            "EbayAdapter must implement IIntegratorAdapter");
        typeof(OzonAdapter).Should().Implement<IIntegratorAdapter>(
            "OzonAdapter must implement IIntegratorAdapter");
        typeof(PttAvmAdapter).Should().Implement<IIntegratorAdapter>(
            "PttAvmAdapter must implement IIntegratorAdapter");
    }

    // ── 6. CRM repositories must live in Infrastructure, not Domain ────────

    [Fact]
    public void CrmRepositories_ShouldResideInInfrastructure()
    {
        typeof(CrmLeadRepository).Namespace.Should()
            .StartWith("MesTech.Infrastructure",
                "CRM repository implementations must live in the Infrastructure layer.");
    }

    // ── 7. CRM domain events must stay in Domain assembly ──────────────────

    [Fact]
    public void CrmDomainEvents_ShouldResideInDomainAssembly()
    {
        var result = Types.InAssembly(typeof(LeadConvertedEvent).Assembly)
            .That()
            .ResideInNamespace("MesTech.Domain.Events.Crm")
            .ShouldNot()
            .HaveDependencyOnAny("MesTech.Application", "MesTech.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "CRM domain events must not reference Application or Infrastructure.");
    }
}
