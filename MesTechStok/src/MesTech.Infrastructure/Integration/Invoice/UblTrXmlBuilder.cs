using System.Xml.Linq;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;

namespace MesTech.Infrastructure.Integration.Invoice;

public class UblTrXmlBuilder : IUblTrXmlBuilder
{
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Inv = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";

    public Task<byte[]> BuildAsync(EInvoiceDocument doc, CancellationToken ct)
    {
        var invoice = new XElement(Inv + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute("xmlns", Inv),
            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "TR1.2.1"),
            new XElement(Cbc + "ProfileID", doc.Scenario.ToString()),
            new XElement(Cbc + "ID", doc.EttnNo),
            new XElement(Cbc + "UUID", doc.GibUuid),
            new XElement(Cbc + "IssueDate", doc.IssueDate.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "InvoiceTypeCode", doc.Type.ToString()),
            new XElement(Cbc + "DocumentCurrencyCode", doc.CurrencyCode),
            BuildPartyElement(true, doc.SellerVkn, doc.SellerTitle),
            BuildPartyElement(false, doc.BuyerVkn ?? string.Empty, doc.BuyerTitle),
            doc.Lines.Select((line, idx) => BuildLineElement(idx + 1, line)),
            BuildMonetaryTotals(doc));

        var xml = new XDocument(new XDeclaration("1.0", "UTF-8", null), invoice);
        using var ms = new MemoryStream();
        xml.Save(ms);
        return Task.FromResult(ms.ToArray());
    }

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

    private XElement BuildLineElement(int lineNum, EInvoiceLine line)
        => new(Cac + "InvoiceLine",
            new XElement(Cbc + "ID", lineNum.ToString()),
            new XElement(Cbc + "InvoicedQuantity",
                new XAttribute("unitCode", line.UnitCode),
                line.Quantity.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", "TRY"),
                line.LineExtensionAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Name", line.Description)),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", "TRY"),
                    line.UnitPrice.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))));

    private XElement BuildMonetaryTotals(EInvoiceDocument doc)
        => new(Cac + "LegalMonetaryTotal",
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", doc.CurrencyCode),
                doc.LineExtensionAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
            new XElement(Cbc + "TaxExclusiveAmount",
                new XAttribute("currencyID", doc.CurrencyCode),
                doc.TaxExclusiveAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
            new XElement(Cbc + "TaxInclusiveAmount",
                new XAttribute("currencyID", doc.CurrencyCode),
                doc.TaxInclusiveAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
            new XElement(Cbc + "PayableAmount",
                new XAttribute("currencyID", doc.CurrencyCode),
                doc.PayableAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)));
}
