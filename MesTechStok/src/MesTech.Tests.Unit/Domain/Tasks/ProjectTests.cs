using FluentAssertions;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Tasks;

/// <summary>
/// Project entity unit testleri.
/// DEV 5 — H27-5.2 (emirname Task 5.2 uyarlanmış gerçek entity'ye gore)
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Tasks")]
public class ProjectTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static Project CreateProject(string name = "Test Proje",
        DateTime? start = null, DateTime? due = null)
        => Project.Create(_tenantId, name, startDate: start, dueDate: due);

    [Fact]
    public void Create_ValidData_ShouldSetStatusToPlanning()
    {
        var p = CreateProject();

        p.Status.Should().Be(ProjectStatus.Planning);
        p.TenantId.Should().Be(_tenantId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyName_ShouldThrow(string name)
    {
        var act = () => Project.Create(_tenantId, name);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithColor_ShouldPreserveColor()
    {
        var p = Project.Create(_tenantId, "Renkli Proje", color: "#FF5733");

        p.Color.Should().Be("#FF5733");
    }

    [Fact]
    public void Start_FromPlanning_ShouldSetStatusToActive()
    {
        var p = CreateProject();
        p.Start();

        p.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void Start_FromCompleted_ShouldThrow()
    {
        var p = CreateProject();
        p.Start();
        p.Complete();

        var act = () => p.Start();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PutOnHold_ActiveProject_ShouldSetStatusToOnHold()
    {
        var p = CreateProject();
        p.Start();
        p.PutOnHold();

        p.Status.Should().Be(ProjectStatus.OnHold);
    }

    [Fact]
    public void Complete_ActiveProject_ShouldSetCompletedAt()
    {
        var p = CreateProject();
        p.Start();
        p.Complete();

        p.Status.Should().Be(ProjectStatus.Completed);
        p.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Start_OnHoldProject_ShouldReturnToActive()
    {
        var p = CreateProject();
        p.Start();
        p.PutOnHold();
        p.Start();   // OnHold → Active tekrar

        p.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void Create_WithOwnerUserId_ShouldSetOwner()
    {
        var ownerId = Guid.NewGuid();
        var p = Project.Create(_tenantId, "Sahipli Proje", ownerUserId: ownerId);

        p.OwnerUserId.Should().Be(ownerId);
    }

    [Fact]
    public void Create_WithStoreId_ShouldSetStoreId()
    {
        var storeId = Guid.NewGuid();
        var p = Project.Create(_tenantId, "Magaza Projesi", storeId: storeId);

        p.StoreId.Should().Be(storeId);
    }

    [Fact]
    public void Create_WithDates_ShouldPreserveDates()
    {
        var start = DateTime.Today;
        var due = DateTime.Today.AddDays(30);
        var p = CreateProject(start: start, due: due);

        p.StartDate.Should().Be(start);
        p.DueDate.Should().Be(due);
    }
}
