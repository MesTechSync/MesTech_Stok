using System.Globalization;
using System.Xml.Linq;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;

namespace MesTech.Infrastructure.Integration.Invoice;

public class UblTrXmlBuilder : IUblTrXmlBuilder
{
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Inv = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly CultureInfo Inv2 = CultureInfo.InvariantCulture;

    // GİB UBL-TR 1.2.1 vergi tipi kodları
    private const string TaxTypeCodeKdv = "0015";
    private const string TaxTypeCodeKdvTevkifat = "9015";
    private const string TaxTypeCodeStopaj = "0003";

    public Task<byte[]> BuildAsync(EInvoiceDocument doc, CancellationToken ct)
    {
        var invoice = new XElement(Inv + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute("xmlns", Inv),
            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "TR1.2.1"),
            new XElement(Cbc + "ProfileID", MapScenarioToProfileId(doc.Scenario)),
            new XElement(Cbc + "ID", doc.EttnNo),
            new XElement(Cbc + "UUID", doc.GibUuid),
            new XElement(Cbc + "IssueDate", doc.IssueDate.ToString("yyyy-MM-dd")),
            // K1b-02: IssueTime — GIB zorunlu alan (UBL-TR 1.2.1)
            new XElement(Cbc + "IssueTime", doc.IssueDate.ToString("HH:mm:ss")),
            new XElement(Cbc + "InvoiceTypeCode", doc.Type.ToString()),
            new XElement(Cbc + "DocumentCurrencyCode", doc.CurrencyCode),
            BuildPartyElement(true, doc.SellerVkn, doc.SellerTitle),
            BuildPartyElement(false, doc.BuyerVkn ?? string.Empty, doc.BuyerTitle),
            // K1b-01: TaxTotal — document-level aggregate per UBL-TR 1.2.1
            BuildTaxTotal(doc),
            doc.Lines.Select((line, idx) => BuildLineElement(idx + 1, line, doc.CurrencyCode)),
            BuildMonetaryTotals(doc));

        var xml = new XDocument(new XDeclaration("1.0", "UTF-8", null), invoice);
        using var ms = new MemoryStream();
        xml.Save(ms);
        return Task.FromResult(ms.ToArray());
    }

    /// <summary>
    /// Maps EInvoiceScenario enum to GIB ProfileID string.
    /// </summary>
    private static string MapScenarioToProfileId(Domain.Enums.EInvoiceScenario scenario)
        => scenario switch
        {
            Domain.Enums.EInvoiceScenario.TEMELFATURA => "TEMELFATURA",
            Domain.Enums.EInvoiceScenario.TICARIFATURA => "TICARIFATURA",
            Domain.Enums.EInvoiceScenario.EARSIVFATURA => "EARSIVFATURA",
            _ => scenario.ToString()
        };

    private XElement BuildPartyElement(bool isSeller, string vkn, string title)
    {
        var tag = isSeller ? "AccountingSupplierParty" : "AccountingCustomerParty";
        return new XElement(Cac + tag,
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID", new XAttribute("schemeID", "VKN"), vkn)),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", title))));
    }

    /// <summary>
    /// K1b-01: Builds document-level TaxTotal element per GIB UBL-TR 1.2.1 schema.
    /// Groups invoice lines by tax percent to produce TaxSubtotal elements.
    /// </summary>
    private XElement BuildTaxTotal(EInvoiceDocument doc)
    {
        var currency = doc.CurrencyCode;

        // Group lines by tax percent to generate TaxSubtotal per rate
        var taxGroups = doc.Lines
            .GroupBy(l => l.TaxPercent)
            .Select(g => new
            {
                TaxPercent = g.Key,
                TaxableAmount = g.Sum(l => l.LineExtensionAmount),
                TaxAmount = g.Sum(l => l.TaxAmount)
            })
            .OrderBy(g => g.TaxPercent)
            .ToList();

        var subtotalElements = taxGroups.Select(g =>
            BuildTaxSubtotal(g.TaxableAmount, g.TaxAmount, g.TaxPercent, currency, "KDV", TaxTypeCodeKdv));

        return new XElement(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", currency),
                doc.TaxAmount.ToString("F2", Inv2)),
            subtotalElements);
    }

    /// <summary>
    /// Builds a single cac:TaxSubtotal element.
    /// Supports both KDV (0015) and withholding tax types (0003, 9015).
    /// </summary>
    private XElement BuildTaxSubtotal(
        decimal taxableAmount,
        decimal taxAmount,
        int percent,
        string currency,
        string taxSchemeName,
        string taxTypeCode)
    {
        return new XElement(Cac + "TaxSubtotal",
            new XElement(Cbc + "TaxableAmount",
                new XAttribute("currencyID", currency),
                taxableAmount.ToString("F2", Inv2)),
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", currency),
                taxAmount.ToString("F2", Inv2)),
            new XElement(Cbc + "Percent", percent.ToString(Inv2)),
            new XElement(Cac + "TaxCategory",
                new XElement(Cac + "TaxScheme",
                    new XElement(Cbc + "Name", taxSchemeName),
                    new XElement(Cbc + "TaxTypeCode", taxTypeCode))));
    }

    /// <summary>
    /// K1b-04: Builds a withholding (tevkifat) TaxTotal element for KDV partial withholding.
    /// Used when WithholdingTaxTotal needs to be added to the invoice.
    /// TaxTypeCode 9015 = KDV Tevkifat, TaxTypeCode 0003 = Stopaj (Gelir Vergisi).
    /// </summary>
    public XElement BuildWithholdingTaxTotal(
        decimal taxableAmount,
        decimal withholdingAmount,
        int withholdingPercent,
        string currency,
        string taxTypeCode = TaxTypeCodeKdvTevkifat)
    {
        var taxSchemeName = taxTypeCode switch
        {
            TaxTypeCodeKdvTevkifat => "KDVTevkifat",
            TaxTypeCodeStopaj => "Stopaj",
            _ => "Tevkifat"
        };

        return new XElement(Cac + "WithholdingTaxTotal",
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", currency),
                withholdingAmount.ToString("F2", Inv2)),
            BuildTaxSubtotal(taxableAmount, withholdingAmount, withholdingPercent,
                currency, taxSchemeName, taxTypeCode));
    }

    private XElement BuildLineElement(int lineNum, EInvoiceLine line, string currency)
        => new(Cac + "InvoiceLine",
            new XElement(Cbc + "ID", lineNum.ToString()),
            new XElement(Cbc + "InvoicedQuantity",
                new XAttribute("unitCode", line.UnitCode),
                line.Quantity.ToString("F4", Inv2)),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", currency),
                line.LineExtensionAmount.ToString("F2", Inv2)),
            // Line-level TaxTotal per UBL-TR 1.2.1
            new XElement(Cac + "TaxTotal",
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", currency),
                    line.TaxAmount.ToString("F2", Inv2)),
                BuildTaxSubtotal(
                    line.LineExtensionAmount,
                    line.TaxAmount,
                    line.TaxPercent,
                    currency,
                    "KDV",
                    TaxTypeCodeKdv)),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Name", line.Description)),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", currency),
                    line.UnitPrice.ToString("F4", Inv2))));

    private XElement BuildMonetaryTotals(EInvoiceDocument doc)
        => new(Cac + "LegalMonetaryTotal",
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", doc.CurrencyCode),
                doc.LineExtensionAmount.ToString("F2", Inv2)),
            new XElement(Cbc + "TaxExclusiveAmount",
                new XAttribute("currencyID", doc.CurrencyCode),
                doc.TaxExclusiveAmount.ToString("F2", Inv2)),
            new XElement(Cbc + "TaxInclusiveAmount",
                new XAttribute("currencyID", doc.CurrencyCode),
                doc.TaxInclusiveAmount.ToString("F2", Inv2)),
            new XElement(Cbc + "PayableAmount",
                new XAttribute("currencyID", doc.CurrencyCode),
                doc.PayableAmount.ToString("F2", Inv2)));
}
