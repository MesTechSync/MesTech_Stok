using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Accounting;

public class CariHareketDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CariHesapId { get; set; }
    public decimal Amount { get; set; }
    public CariDirection Direction { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
