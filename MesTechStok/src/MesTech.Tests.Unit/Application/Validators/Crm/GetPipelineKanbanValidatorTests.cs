using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetPipelineKanbanValidatorTests
{
    private readonly GetPipelineKanbanValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var input = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyPipelineId_ShouldFail()
    {
        var input = CreateValidQuery() with { PipelineId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PipelineId");
    }

    private static GetPipelineKanbanQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), PipelineId: Guid.NewGuid());
}
