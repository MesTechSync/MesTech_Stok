using System.Xml.Linq;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Validates UBL-TR 1.2.1 e-invoice XML against GİB mandatory field requirements.
/// Reference: GİB UBL-TR Teknik Kılavuz v1.2.1 — zorunlu alan listesi.
/// </summary>
public sealed class UblTrXmlValidator : IUblTrXmlValidator
{
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    public Task<IReadOnlyList<string>> ValidateAsync(byte[] xmlBytes, CancellationToken ct = default)
    {
        var errors = new List<string>();

        XDocument doc;
        try
        {
            using var ms = new MemoryStream(xmlBytes);
            doc = XDocument.Load(ms);
        }
        catch (Exception ex)
        {
            errors.Add($"XML parse hatası: {ex.Message}");
            return Task.FromResult<IReadOnlyList<string>>(errors);
        }

        var root = doc.Root;
        if (root is null)
        {
            errors.Add("XML root element bulunamadı.");
            return Task.FromResult<IReadOnlyList<string>>(errors);
        }

        // 1. UBLVersionID = "2.1"
        var ublVersion = root.Element(Cbc + "UBLVersionID")?.Value;
        if (string.IsNullOrWhiteSpace(ublVersion))
            errors.Add("UBLVersionID zorunlu alan eksik.");
        else if (ublVersion != "2.1")
            errors.Add($"UBLVersionID '2.1' olmalı, bulunan: '{ublVersion}'.");

        // 2. CustomizationID — TR1.2 veya TR1.2.1
        var customization = root.Element(Cbc + "CustomizationID")?.Value;
        if (string.IsNullOrWhiteSpace(customization))
            errors.Add("CustomizationID zorunlu alan eksik.");
        else if (!customization.StartsWith("TR1.2", StringComparison.Ordinal))
            errors.Add($"CustomizationID 'TR1.2' ile başlamalı, bulunan: '{customization}'.");

        // 3. ProfileID — TEMELFATURA, TICARIFATURA veya EARSIVFATURA
        var profileId = root.Element(Cbc + "ProfileID")?.Value;
        if (string.IsNullOrWhiteSpace(profileId))
            errors.Add("ProfileID zorunlu alan eksik.");
        else if (profileId is not ("TEMELFATURA" or "TICARIFATURA" or "EARSIVFATURA"))
            errors.Add($"ProfileID geçersiz: '{profileId}'. TEMELFATURA/TICARIFATURA/EARSIVFATURA olmalı.");

        // 4. ID (ETTN)
        if (string.IsNullOrWhiteSpace(root.Element(Cbc + "ID")?.Value))
            errors.Add("ID (ETTN numarası) zorunlu alan eksik.");

        // 5. UUID (GİB UUID)
        var uuid = root.Element(Cbc + "UUID")?.Value;
        if (string.IsNullOrWhiteSpace(uuid))
            errors.Add("UUID (GİB UUID) zorunlu alan eksik.");
        else if (!Guid.TryParse(uuid, out _))
            errors.Add($"UUID geçerli bir GUID formatında olmalı: '{uuid}'.");

        // 6. IssueDate
        if (string.IsNullOrWhiteSpace(root.Element(Cbc + "IssueDate")?.Value))
            errors.Add("IssueDate zorunlu alan eksik.");

        // 7. IssueTime — GİB UBL-TR 1.2.1 zorunlu
        if (string.IsNullOrWhiteSpace(root.Element(Cbc + "IssueTime")?.Value))
            errors.Add("IssueTime zorunlu alan eksik (UBL-TR 1.2.1 gereksinimi).");

        // 8. InvoiceTypeCode
        var typeCode = root.Element(Cbc + "InvoiceTypeCode")?.Value;
        if (string.IsNullOrWhiteSpace(typeCode))
            errors.Add("InvoiceTypeCode zorunlu alan eksik.");

        // 9. DocumentCurrencyCode
        if (string.IsNullOrWhiteSpace(root.Element(Cbc + "DocumentCurrencyCode")?.Value))
            errors.Add("DocumentCurrencyCode zorunlu alan eksik.");

        // 10. AccountingSupplierParty (Satıcı)
        ValidateParty(root, "AccountingSupplierParty", "Satıcı", errors);

        // 11. AccountingCustomerParty (Alıcı)
        ValidateParty(root, "AccountingCustomerParty", "Alıcı", errors);

        // 12. TaxTotal
        var taxTotal = root.Element(Cac + "TaxTotal");
        if (taxTotal is null)
            errors.Add("TaxTotal zorunlu alan eksik.");
        else
        {
            if (string.IsNullOrWhiteSpace(taxTotal.Element(Cbc + "TaxAmount")?.Value))
                errors.Add("TaxTotal/TaxAmount zorunlu alan eksik.");

            var taxSubtotals = taxTotal.Elements(Cac + "TaxSubtotal").ToList();
            if (taxSubtotals.Count == 0)
                errors.Add("TaxTotal en az bir TaxSubtotal içermelidir.");

            foreach (var sub in taxSubtotals)
            {
                var taxScheme = sub.Element(Cac + "TaxCategory")?.Element(Cac + "TaxScheme");
                if (taxScheme is null)
                    errors.Add("TaxSubtotal/TaxCategory/TaxScheme zorunlu alan eksik.");
                else if (string.IsNullOrWhiteSpace(taxScheme.Element(Cbc + "TaxTypeCode")?.Value))
                    errors.Add("TaxScheme/TaxTypeCode zorunlu alan eksik (ör. 0015=KDV).");
            }
        }

        // 13. LegalMonetaryTotal
        var monetary = root.Element(Cac + "LegalMonetaryTotal");
        if (monetary is null)
            errors.Add("LegalMonetaryTotal zorunlu alan eksik.");
        else
        {
            ValidateMonetaryField(monetary, "LineExtensionAmount", errors);
            ValidateMonetaryField(monetary, "TaxExclusiveAmount", errors);
            ValidateMonetaryField(monetary, "TaxInclusiveAmount", errors);
            ValidateMonetaryField(monetary, "PayableAmount", errors);
        }

        // 14. InvoiceLine — en az 1 satır
        var lines = root.Elements(Cac + "InvoiceLine").ToList();
        if (lines.Count == 0)
            errors.Add("En az bir InvoiceLine zorunludur.");

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.Element(Cbc + "ID")?.Value))
                errors.Add("InvoiceLine/ID zorunlu alan eksik.");
            if (string.IsNullOrWhiteSpace(line.Element(Cbc + "InvoicedQuantity")?.Value))
                errors.Add("InvoiceLine/InvoicedQuantity zorunlu alan eksik.");

            var item = line.Element(Cac + "Item");
            if (item is null || string.IsNullOrWhiteSpace(item.Element(Cbc + "Name")?.Value))
                errors.Add("InvoiceLine/Item/Name zorunlu alan eksik.");
        }

        return Task.FromResult<IReadOnlyList<string>>(errors);
    }

    private void ValidateParty(XElement root, string partyTag, string label, List<string> errors)
    {
        var partyWrapper = root.Element(Cac + partyTag);
        if (partyWrapper is null)
        {
            errors.Add($"{label} ({partyTag}) zorunlu alan eksik.");
            return;
        }

        var party = partyWrapper.Element(Cac + "Party");
        if (party is null)
        {
            errors.Add($"{label} Party elementi eksik.");
            return;
        }

        var identification = party.Element(Cac + "PartyIdentification");
        if (identification is null || string.IsNullOrWhiteSpace(identification.Element(Cbc + "ID")?.Value))
            errors.Add($"{label} VKN/TCKN (PartyIdentification/ID) zorunlu alan eksik.");

        var partyName = party.Element(Cac + "PartyName");
        if (partyName is null || string.IsNullOrWhiteSpace(partyName.Element(Cbc + "Name")?.Value))
            errors.Add($"{label} unvanı (PartyName/Name) zorunlu alan eksik.");
    }

    private static void ValidateMonetaryField(XElement monetary, string fieldName, List<string> errors)
    {
        var element = monetary.Element(Cbc + fieldName);
        if (element is null || string.IsNullOrWhiteSpace(element.Value))
            errors.Add($"LegalMonetaryTotal/{fieldName} zorunlu alan eksik.");
        else if (!decimal.TryParse(element.Value, System.Globalization.NumberStyles.Any,
                     System.Globalization.CultureInfo.InvariantCulture, out _))
            errors.Add($"LegalMonetaryTotal/{fieldName} geçerli bir sayısal değer olmalı.");
    }
}
