namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// PttAVM seller payment report JSON models.
/// Commission = PttAVM marketplace commission (category-based %).
/// </summary>
internal sealed class PttAvmSettlementLine
{
    public string? OrderId { get; set; }
    public decimal ProductAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CargoAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string? PaymentDate { get; set; }
}
