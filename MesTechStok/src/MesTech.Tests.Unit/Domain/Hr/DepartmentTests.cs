using FluentAssertions;
using MesTech.Domain.Entities.Hr;

namespace MesTech.Tests.Unit.Domain.Hr;

/// <summary>
/// Department entity domain logic tests — H28 DEV5 T5.5
/// </summary>
public class DepartmentTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidName_ShouldSucceed()
    {
        var dept = Department.Create(_tenantId, "Yazılım");
        dept.Name.Should().Be("Yazılım");
        dept.ManagerEmployeeId.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        var act = () => Department.Create(_tenantId, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetManager_ShouldUpdateManagerId()
    {
        var dept = Department.Create(_tenantId, "Pazarlama");
        var managerId = Guid.NewGuid();
        dept.SetManager(managerId);
        dept.ManagerEmployeeId.Should().Be(managerId);
    }

    [Fact]
    public void Rename_ShouldUpdateName()
    {
        var dept = Department.Create(_tenantId, "Eski");
        dept.Rename("Yeni");
        dept.Name.Should().Be("Yeni");
    }

    [Fact]
    public void Create_WithParent_ShouldSetParentId()
    {
        var parentId = Guid.NewGuid();
        var dept = Department.Create(_tenantId, "Alt Departman", parentId);
        dept.ParentDepartmentId.Should().Be(parentId);
    }
}
