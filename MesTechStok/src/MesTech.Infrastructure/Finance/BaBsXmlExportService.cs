using System.Text;
using System.Xml;
using MesTech.Application.Interfaces.Accounting;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Finance;

/// <summary>
/// GiB Ba/Bs formu XML export servisi.
/// VUK 396 Sira No'lu Genel Teblig formatina uygun XML uretir.
/// FormTipi: "Ba" = alis bildirimi, "Bs" = satis bildirimi.
/// </summary>
public sealed class BaBsXmlExportService : IBaBsXmlExportService
{
    private static readonly HashSet<string> ValidFormTypes = new(StringComparer.OrdinalIgnoreCase) { "Ba", "Bs" };

    private readonly ILogger<BaBsXmlExportService> _logger;

    public BaBsXmlExportService(ILogger<BaBsXmlExportService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<byte[]> ExportToXmlAsync(
        BaBsReportDto report,
        string formType,
        int year,
        int month,
        string tenantVKN,
        string tenantName)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(formType);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantVKN);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantName);

        if (!ValidFormTypes.Contains(formType))
            throw new ArgumentException($"FormTipi 'Ba' veya 'Bs' olmalidir. Verilen: '{formType}'", nameof(formType));
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Ay 1-12 araliginda olmalidir.");
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Yil 2000-2100 araliginda olmalidir.");

        // Normalize form type casing
        var normalizedFormType = formType.Equals("Ba", StringComparison.OrdinalIgnoreCase) ? "Ba" : "Bs";

        // Select entries based on form type
        var entries = normalizedFormType == "Ba" ? report.BaEntries : report.BsEntries;

        var xmlBytes = GenerateXml(normalizedFormType, year, month, tenantVKN, tenantName, entries);

        _logger.LogInformation(
            "BaBs XML olusturuldu — FormTipi={FormType}, {Year}/{Month:D2}, VKN={VKN}, Satir={Count}",
            normalizedFormType, year, month, tenantVKN, entries.Count);

        return Task.FromResult(xmlBytes);
    }

    private static byte[] GenerateXml(
        string formType,
        int year,
        int month,
        string tenantVKN,
        string tenantName,
        List<BaBsCounterpartyDto> entries)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false), // UTF-8 without BOM
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false
        };

        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();

            // <BaBsForm>
            writer.WriteStartElement("BaBsForm");

            // <Donem>
            writer.WriteStartElement("Donem");
            writer.WriteElementString("Yil", year.ToString());
            writer.WriteElementString("Ay", month.ToString("D2"));
            writer.WriteEndElement(); // </Donem>

            // <FormTipi>
            writer.WriteElementString("FormTipi", formType);

            // <MukellefBilgileri>
            writer.WriteStartElement("MukellefBilgileri");
            writer.WriteElementString("VKN", tenantVKN);
            writer.WriteElementString("Unvan", tenantName);
            writer.WriteEndElement(); // </MukellefBilgileri>

            // <BildirimSatirlari>
            writer.WriteStartElement("BildirimSatirlari");

            foreach (var entry in entries)
            {
                writer.WriteStartElement("Satir");
                writer.WriteElementString("KarsiTarafVKN", entry.VKN);
                writer.WriteElementString("KarsiTarafUnvan", entry.Name);
                writer.WriteElementString("UlkeKodu", "TR");
                writer.WriteElementString("BelgeSayisi", entry.DocumentCount.ToString());
                writer.WriteElementString("ToplamTutar", entry.TotalAmount.ToString("F2"));
                writer.WriteEndElement(); // </Satir>
            }

            writer.WriteEndElement(); // </BildirimSatirlari>

            writer.WriteEndElement(); // </BaBsForm>
            writer.WriteEndDocument();
        }

        return stream.ToArray();
    }
}
