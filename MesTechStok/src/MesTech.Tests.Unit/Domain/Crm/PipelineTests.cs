using FluentAssertions;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Crm;

public class PipelineTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void CreatePipeline_ShouldSetDefaults()
    {
        var pipeline = Pipeline.Create(_tenantId, "Satış Hunisi", true, 1);
        pipeline.Name.Should().Be("Satış Hunisi");
        pipeline.IsDefault.Should().BeTrue();
        pipeline.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void CreatePipeline_WithEmptyName_ShouldThrow()
    {
        var act = () => Pipeline.Create(_tenantId, "", false, 1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RenamePipeline_ShouldUpdateName()
    {
        var pipeline = Pipeline.Create(_tenantId, "Eski İsim", false, 1);
        pipeline.Rename("Yeni İsim");
        pipeline.Name.Should().Be("Yeni İsim");
    }

    [Fact]
    public void CreateStage_WithProbabilityOutOfRange_ShouldThrow()
    {
        var act = () => PipelineStage.Create(_tenantId, Guid.NewGuid(), "Stage", 1, 150m, StageType.Normal);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateStage_WithNullProbability_ShouldSucceed()
    {
        var stage = PipelineStage.Create(_tenantId, Guid.NewGuid(), "Girişim", 1, null, StageType.Normal);
        stage.Probability.Should().BeNull();
    }

    [Fact]
    public void CreateStage_WonType_ShouldSetTypeToWon()
    {
        var stage = PipelineStage.Create(_tenantId, Guid.NewGuid(), "Kapandı", 5, 100m, StageType.Won);
        stage.Type.Should().Be(StageType.Won);
    }
}
