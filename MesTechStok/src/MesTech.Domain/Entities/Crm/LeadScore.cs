using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Crm;

/// <summary>
/// Lead puanlama — musteri adayinin sicaklik derecesi.
/// Puan kaynaklari ScoreBreakdownJson'da saklanir.
/// Hot(>=80), Warm(>=50), Cold(&lt;50).
/// </summary>
public sealed class LeadScore : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid LeadId { get; private set; }
    public int Score { get; private set; }
    public LeadTemperature Temperature { get; private set; }
    public string ScoreBreakdownJson { get; private set; } = "{}";
    public DateTime LastScoredAt { get; private set; }

    private LeadScore() { }

    public static LeadScore Create(Guid tenantId, Guid leadId, int initialScore = 0)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        if (leadId == Guid.Empty)
            throw new ArgumentException("LeadId boş olamaz.", nameof(leadId));

        var score = Math.Clamp(initialScore, 0, 100);
        return new LeadScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LeadId = leadId,
            Score = score,
            Temperature = ClassifyTemperature(score),
            LastScoredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddPoints(string reason, int points)
    {
        Score = Math.Clamp(Score + points, 0, 100);
        Temperature = ClassifyTemperature(Score);
        LastScoredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBreakdown(string breakdownJson)
    {
        ScoreBreakdownJson = breakdownJson ?? "{}";
        UpdatedAt = DateTime.UtcNow;
    }

    private static LeadTemperature ClassifyTemperature(int score) => score switch
    {
        >= 80 => LeadTemperature.Hot,
        >= 50 => LeadTemperature.Warm,
        _ => LeadTemperature.Cold
    };
}

public enum LeadTemperature { Cold = 0, Warm = 1, Hot = 2 }
