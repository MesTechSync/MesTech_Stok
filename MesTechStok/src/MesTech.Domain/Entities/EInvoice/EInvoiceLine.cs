using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.EInvoice;

public class EInvoiceLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EInvoiceDocumentId { get; private set; }
    public int LineNumber { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string UnitCode { get; private set; } = "C62";
    public decimal UnitPrice { get; private set; }
    public decimal LineExtensionAmount { get; private set; }
    public decimal AllowanceAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public int TaxPercent { get; private set; }
    public string? ProductCode { get; private set; }
    public Guid? ProductId { get; private set; }

    public static EInvoiceLine Create(
        Guid documentId,
        int lineNumber,
        string description,
        decimal quantity,
        string unitCode,
        decimal unitPrice,
        int taxPercent,
        decimal allowanceAmount,
        Guid? productId)
    {
        var taxBase = quantity * unitPrice - allowanceAmount;
        var taxAmount = taxBase * taxPercent / 100m;

        return new EInvoiceLine
        {
            EInvoiceDocumentId = documentId,
            LineNumber = lineNumber,
            Description = description,
            Quantity = quantity,
            UnitCode = unitCode,
            UnitPrice = unitPrice,
            LineExtensionAmount = taxBase,
            AllowanceAmount = allowanceAmount,
            TaxPercent = taxPercent,
            TaxAmount = taxAmount,
            ProductId = productId
        };
    }
}
