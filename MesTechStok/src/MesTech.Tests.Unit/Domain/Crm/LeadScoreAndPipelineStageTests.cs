using FluentAssertions;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain.Crm;

/// <summary>
/// Sprint 3 — LeadScore + PipelineStage entity testleri.
/// AddPoints, Temperature, clamp, sıralama, Won/Lost.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Crm")]
public class LeadScoreAndPipelineStageTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // ══════════════════════════════════════
    // LeadScore
    // ══════════════════════════════════════

    [Fact]
    public void LeadScore_Create_ShouldSetInitialScore()
    {
        var ls = LeadScore.Create(TenantId, Guid.NewGuid(), 50);
        ls.Score.Should().Be(50);
        ls.Temperature.Should().Be(LeadTemperature.Warm);
    }

    [Fact]
    public void LeadScore_Create_DefaultZero_ShouldBeCold()
    {
        var ls = LeadScore.Create(TenantId, Guid.NewGuid());
        ls.Score.Should().Be(0);
        ls.Temperature.Should().Be(LeadTemperature.Cold);
    }

    [Fact]
    public void LeadScore_Create_EmptyTenantId_ShouldThrow()
    {
        var act = () => LeadScore.Create(Guid.Empty, Guid.NewGuid());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LeadScore_Create_EmptyLeadId_ShouldThrow()
    {
        var act = () => LeadScore.Create(TenantId, Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LeadScore_AddPoints_ShouldIncrease()
    {
        var ls = LeadScore.Create(TenantId, Guid.NewGuid(), 30);
        ls.AddPoints("Web form doldurdu", 25);

        ls.Score.Should().Be(55);
        ls.Temperature.Should().Be(LeadTemperature.Warm);
    }

    [Fact]
    public void LeadScore_AddPoints_ShouldCrossToHot()
    {
        var ls = LeadScore.Create(TenantId, Guid.NewGuid(), 70);
        ls.AddPoints("Demo istedi", 15);

        ls.Score.Should().Be(85);
        ls.Temperature.Should().Be(LeadTemperature.Hot);
    }

    [Fact]
    public void LeadScore_AddPoints_ClampAt100()
    {
        var ls = LeadScore.Create(TenantId, Guid.NewGuid(), 95);
        ls.AddPoints("Fiyat sordu", 20);

        ls.Score.Should().Be(100, "clamp max 100");
        ls.Temperature.Should().Be(LeadTemperature.Hot);
    }

    [Fact]
    public void LeadScore_AddNegativePoints_ClampAt0()
    {
        var ls = LeadScore.Create(TenantId, Guid.NewGuid(), 10);
        ls.AddPoints("Yanıt vermedi", -30);

        ls.Score.Should().Be(0, "clamp min 0");
        ls.Temperature.Should().Be(LeadTemperature.Cold);
    }

    [Fact]
    public void LeadScore_Create_Over100_ShouldClamp()
    {
        var ls = LeadScore.Create(TenantId, Guid.NewGuid(), 200);
        ls.Score.Should().Be(100);
    }

    [Fact]
    public void LeadScore_Create_Negative_ShouldClamp()
    {
        var ls = LeadScore.Create(TenantId, Guid.NewGuid(), -50);
        ls.Score.Should().Be(0);
    }

    [Theory]
    [InlineData(0, LeadTemperature.Cold)]
    [InlineData(49, LeadTemperature.Cold)]
    [InlineData(50, LeadTemperature.Warm)]
    [InlineData(79, LeadTemperature.Warm)]
    [InlineData(80, LeadTemperature.Hot)]
    [InlineData(100, LeadTemperature.Hot)]
    public void LeadScore_Temperature_BoundaryValues(int score, LeadTemperature expected)
    {
        var ls = LeadScore.Create(TenantId, Guid.NewGuid(), score);
        ls.Temperature.Should().Be(expected);
    }

    [Fact]
    public void LeadScore_SetBreakdown_ShouldStoreJson()
    {
        var ls = LeadScore.Create(TenantId, Guid.NewGuid());
        ls.SetBreakdown("{\"web_form\":10,\"demo_request\":20}");

        ls.ScoreBreakdownJson.Should().Contain("web_form");
    }

    [Fact]
    public void LeadScore_SetBreakdown_Null_ShouldDefault()
    {
        var ls = LeadScore.Create(TenantId, Guid.NewGuid());
        ls.SetBreakdown(null!);

        ls.ScoreBreakdownJson.Should().Be("{}");
    }

    // ══════════════════════════════════════
    // PipelineStage
    // ══════════════════════════════════════

    [Fact]
    public void PipelineStage_Create_Valid_ShouldSucceed()
    {
        var ps = PipelineStage.Create(TenantId, Guid.NewGuid(),
            "Görüşme", 2, 30m, StageType.Normal, "#3498DB");

        ps.Name.Should().Be("Görüşme");
        ps.Position.Should().Be(2);
        ps.Probability.Should().Be(30m);
        ps.Type.Should().Be(StageType.Normal);
        ps.Color.Should().Be("#3498DB");
    }

    [Fact]
    public void PipelineStage_Create_EmptyName_ShouldThrow()
    {
        var act = () => PipelineStage.Create(TenantId, Guid.NewGuid(),
            "", 1, 50m, StageType.Normal);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PipelineStage_Create_ProbabilityOver100_ShouldThrow()
    {
        var act = () => PipelineStage.Create(TenantId, Guid.NewGuid(),
            "Kapandı", 5, 150m, StageType.Won);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PipelineStage_Create_NegativeProbability_ShouldThrow()
    {
        var act = () => PipelineStage.Create(TenantId, Guid.NewGuid(),
            "Kayıp", 6, -10m, StageType.Lost);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PipelineStage_Won_ShouldHaveType()
    {
        var ps = PipelineStage.Create(TenantId, Guid.NewGuid(),
            "Kazanıldı", 10, 100m, StageType.Won, "#27AE60");

        ps.Type.Should().Be(StageType.Won);
        ps.Probability.Should().Be(100m);
    }

    [Fact]
    public void PipelineStage_Lost_ShouldHaveType()
    {
        var ps = PipelineStage.Create(TenantId, Guid.NewGuid(),
            "Kaybedildi", 11, 0m, StageType.Lost, "#E74C3C");

        ps.Type.Should().Be(StageType.Lost);
    }

    [Fact]
    public void PipelineStage_UpdatePosition_ShouldChange()
    {
        var ps = PipelineStage.Create(TenantId, Guid.NewGuid(),
            "Teklif", 3, 60m, StageType.Normal);
        ps.UpdatePosition(5);

        ps.Position.Should().Be(5);
    }

    [Fact]
    public void PipelineStage_NullProbability_ShouldBeAllowed()
    {
        var ps = PipelineStage.Create(TenantId, Guid.NewGuid(),
            "Custom", 1, null, StageType.Normal);

        ps.Probability.Should().BeNull();
    }
}
