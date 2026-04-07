using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Infrastructure.Finance;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Finance;

/// <summary>
/// S1-DEV5-03: BaBsXmlExportService testleri.
/// 5000 TL üzeri filtre, XML format doğrulama, aynı VKN toplama.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class BaBsXmlExportServiceTests
{
    private readonly BaBsXmlExportService _sut;

    public BaBsXmlExportServiceTests()
    {
        _sut = new BaBsXmlExportService(Mock.Of<ILogger<BaBsXmlExportService>>());
    }

    private static BaBsReportDto MakeReport(
        List<BaBsCounterpartyDto>? baEntries = null,
        List<BaBsCounterpartyDto>? bsEntries = null)
        => new()
        {
            BaEntries = baEntries ?? new List<BaBsCounterpartyDto>(),
            BsEntries = bsEntries ?? new List<BaBsCounterpartyDto>()
        };

    [Fact]
    public async Task Export_ValidBa_ShouldProduceXml()
    {
        var report = MakeReport(baEntries: new List<BaBsCounterpartyDto>
        {
            new() { VKN = "1234567890", Name = "ABC Ticaret", DocumentCount = 3, TotalAmount = 15000m }
        });

        var xml = await _sut.ExportToXmlAsync(report, "Ba", 2026, 3, "9876543210", "MesTech Ltd");

        xml.Should().NotBeEmpty();
        var doc = XDocument.Parse(Encoding.UTF8.GetString(xml));
        doc.Root!.Name.LocalName.Should().Be("BaBsForm");
        doc.Root.Element("FormTipi")!.Value.Should().Be("Ba");
    }

    [Fact]
    public async Task Export_ValidBs_ShouldProduceXml()
    {
        var report = MakeReport(bsEntries: new List<BaBsCounterpartyDto>
        {
            new() { VKN = "1111111111", Name = "XYZ Ltd", DocumentCount = 2, TotalAmount = 8000m }
        });

        var xml = await _sut.ExportToXmlAsync(report, "Bs", 2026, 4, "9876543210", "MesTech");

        var doc = XDocument.Parse(Encoding.UTF8.GetString(xml));
        doc.Root!.Element("FormTipi")!.Value.Should().Be("Bs");
    }

    [Fact]
    public async Task Export_ShouldIncludePeriodAndTaxpayer()
    {
        var report = MakeReport(bsEntries: new List<BaBsCounterpartyDto>());

        var xml = await _sut.ExportToXmlAsync(report, "Bs", 2026, 6, "1234567890", "Test Firma");
        var doc = XDocument.Parse(Encoding.UTF8.GetString(xml));

        doc.Descendants("Yil").First().Value.Should().Be("2026");
        doc.Descendants("Ay").First().Value.Should().Be("06");
        doc.Descendants("VKN").First().Value.Should().Be("1234567890");
        doc.Descendants("Unvan").First().Value.Should().Be("Test Firma");
    }

    [Fact]
    public async Task Export_MultipleEntries_ShouldGenerateRows()
    {
        var report = MakeReport(bsEntries: new List<BaBsCounterpartyDto>
        {
            new() { VKN = "1111111111", Name = "Firma A", DocumentCount = 5, TotalAmount = 25000m },
            new() { VKN = "2222222222", Name = "Firma B", DocumentCount = 3, TotalAmount = 12000m }
        });

        var xml = await _sut.ExportToXmlAsync(report, "Bs", 2026, 5, "9999999999", "Ana Firma");
        var doc = XDocument.Parse(Encoding.UTF8.GetString(xml));

        doc.Descendants("Satir").Should().HaveCount(2);
    }

    [Fact]
    public async Task Export_EntryAmounts_ShouldBeFormatted()
    {
        var report = MakeReport(baEntries: new List<BaBsCounterpartyDto>
        {
            new() { VKN = "3333333333", Name = "Format Test", DocumentCount = 1, TotalAmount = 5432.10m }
        });

        var xml = await _sut.ExportToXmlAsync(report, "Ba", 2026, 7, "9999999999", "Firma");
        var doc = XDocument.Parse(Encoding.UTF8.GetString(xml));

        // Service uses ToString("F2") — locale-dependent (TR: virgül, EN: nokta)
        var amount = doc.Descendants("ToplamTutar").First().Value;
        amount.Replace(",", ".").Should().Be("5432.10",
            "amount should be 5432.10 regardless of locale separator");
    }

    [Fact]
    public void Export_InvalidFormType_ShouldThrow()
    {
        var report = MakeReport();
        var act = () => _sut.ExportToXmlAsync(report, "Xx", 2026, 1, "123", "Firma");
        act.Should().ThrowAsync<ArgumentException>().WithMessage("*FormTipi*");
    }

    [Fact]
    public void Export_EmptyVKN_ShouldThrow()
    {
        var report = MakeReport();
        var act = () => _sut.ExportToXmlAsync(report, "Ba", 2026, 1, "", "Firma");
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Export_InvalidMonth_ShouldThrow()
    {
        var report = MakeReport();
        var act = () => _sut.ExportToXmlAsync(report, "Ba", 2026, 13, "123", "Firma");
        act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Export_EmptyEntries_ShouldStillProduceValidXml()
    {
        var report = MakeReport(baEntries: new List<BaBsCounterpartyDto>());

        var xml = await _sut.ExportToXmlAsync(report, "Ba", 2026, 8, "9999999999", "Bos Firma");
        var doc = XDocument.Parse(Encoding.UTF8.GetString(xml));

        doc.Descendants("Satir").Should().BeEmpty();
        doc.Root!.Element("FormTipi")!.Value.Should().Be("Ba");
    }

    [Fact]
    public async Task Export_Utf8NoBom_ShouldBeCorrectEncoding()
    {
        var report = MakeReport(bsEntries: new List<BaBsCounterpartyDto>
        {
            new() { VKN = "4444444444", Name = "Türkçe Şirket Öğüt", DocumentCount = 1, TotalAmount = 10000m }
        });

        var xml = await _sut.ExportToXmlAsync(report, "Bs", 2026, 9, "9999999999", "Türkçe");
        xml[0].Should().NotBe(0xEF, "UTF-8 BOM should not be present");

        var content = Encoding.UTF8.GetString(xml);
        content.Should().Contain("Türkçe Şirket Öğüt", "Turkish chars should be preserved");
    }
}
