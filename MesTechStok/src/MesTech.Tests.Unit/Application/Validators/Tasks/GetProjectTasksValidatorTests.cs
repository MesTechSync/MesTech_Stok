using FluentAssertions;
using MesTech.Application.Features.Tasks.Queries.GetProjectTasks;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Tasks;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetProjectTasksValidatorTests
{
    private readonly GetProjectTasksValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyProjectId_ShouldFail()
    {
        var input = CreateValidQuery() with { ProjectId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProjectId");
    }

    private static GetProjectTasksQuery CreateValidQuery() => new(ProjectId: Guid.NewGuid());
}
