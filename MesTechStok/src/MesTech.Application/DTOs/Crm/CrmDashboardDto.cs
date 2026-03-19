namespace MesTech.Application.DTOs.Crm;

public class CrmDashboardDto
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int VipCustomers { get; set; }
    public int TotalSuppliers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenDeals { get; set; }
    public decimal PipelineValue { get; set; }
    public int UnreadMessages { get; set; }
    public int TotalMessages { get; set; }
    public IReadOnlyList<StageSummaryDto> StageSummaries { get; set; } = [];
    public IReadOnlyList<RecentActivityDto> RecentActivities { get; set; } = [];
}

public class StageSummaryDto
{
    public Guid StageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string? StageColor { get; set; }
    public int DealCount { get; set; }
    public decimal TotalValue { get; set; }
}

public class RecentActivityDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? ContactName { get; set; }
}
