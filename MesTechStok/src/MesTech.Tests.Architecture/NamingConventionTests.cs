using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CreateProject;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using MesTech.Domain.Events.Crm;
using MesTech.Domain.Events.Tasks;
using MesTech.Infrastructure.Persistence.Repositories;
using MesTech.Infrastructure.Persistence.Repositories.Crm;
using NetArchTest.Rules;

namespace MesTech.Tests.Architecture;

[Trait("Category", "Architecture")]
public class NamingConventionTests
{
    // ── 1. Domain events must end with "Event" ─────────────────────────────

    [Fact]
    public void DomainEvents_ShouldEndWithEventSuffix()
    {
        var domainAssembly = typeof(LeadConvertedEvent).Assembly;

        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Domain.Events")
            .Should()
            .HaveNameEndingWith("Event")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "All classes/records in MesTech.Domain.Events.* must end with 'Event'.");
    }

    // ── 2. Application commands must end with "Command" ────────────────────

    [Fact]
    public void Commands_ShouldEndWithCommandSuffix()
    {
        var applicationAssembly = typeof(CreateWorkTaskCommand).Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .That()
            .HaveNameEndingWith("Command")
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "All types named *Command must follow the Command suffix convention.");
    }

    // ── 3. Application handlers must end with "Handler" ────────────────────

    [Fact]
    public void Handlers_ShouldEndWithHandlerSuffix()
    {
        var applicationAssembly = typeof(CreateWorkTaskHandler).Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "All types named *Handler must follow the Handler suffix convention.");
    }

    // ── 4. Repository implementations must end with "Repository" ──────────

    [Fact]
    public void Repositories_ShouldEndWithRepositorySuffix()
    {
        var infraAssembly = typeof(CrmLeadRepository).Assembly;

        var result = Types.InAssembly(infraAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Infrastructure.Persistence.Repositories")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "All concrete classes in the Repositories namespace must end with 'Repository'.");
    }

    // ── 5. Task-layer events must end with "Event" (sub-namespace check) ───

    [Fact]
    public void TaskDomainEvents_ShouldEndWithEventSuffix()
    {
        var domainAssembly = typeof(TaskCompletedEvent).Assembly;

        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespace("MesTech.Domain.Events.Tasks")
            .Should()
            .HaveNameEndingWith("Event")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "All Task domain events must end with 'Event'.");
    }

    // ── 6. Project-level handlers reside in Application, not Infrastructure ─

    [Fact]
    public void Handlers_ShouldResideInApplicationAssembly()
    {
        typeof(CreateProjectHandler).Assembly.GetName().Name
            .Should().Be("MesTech.Application",
                "Handlers must live in the Application assembly, not Infrastructure.");
    }
}
