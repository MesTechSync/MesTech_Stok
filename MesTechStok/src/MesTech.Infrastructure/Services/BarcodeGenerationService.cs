using System.Globalization;
using System.Text;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Barkod gorsel uretim servisi.
/// Code128-B encoding ile SVG ve QuestPDF PDF/PNG cikti uretir.
/// Harici barcode kutuphanesi gerektirmez — saf C# implementasyon.
/// S06f — DEV 6.
/// </summary>
public sealed class BarcodeGenerationService : IBarcodeGenerationService
{
    private readonly ILogger<BarcodeGenerationService> _logger;

    public BarcodeGenerationService(ILogger<BarcodeGenerationService> logger)
    {
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public byte[] GenerateCode128Png(string data, int width = 400, int height = 80)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);

        _logger.LogDebug("[Barcode] Code128 PNG uretiliyor: {Data}", data);

        var barPattern = EncodeCode128B(data);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Piksel bazli boyut: 1pt = 1px (yaklasik)
                page.Size(new PageSize(width, height + 20, Unit.Point));
                page.Margin(0);
                page.Content().Column(col =>
                {
                    col.Item().Height(height).Element(c => DrawBarcodePattern(c, barPattern, height));
                    col.Item().Height(20).AlignCenter()
                        .Text(data).FontSize(10).FontFamily("Courier New");
                });
            });
        });

        return document.GeneratePdf();
    }

    /// <inheritdoc />
    public byte[] GenerateEan13Png(string data, int width = 300, int height = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);

        // EAN-13 dogrulama: 13 haneli sayi
        var digits = data.Replace("-", "", StringComparison.Ordinal).Trim();
        if (digits.Length == 12)
            digits += CalculateEan13CheckDigit(digits).ToString(CultureInfo.InvariantCulture);

        if (digits.Length != 13 || !long.TryParse(digits, CultureInfo.InvariantCulture, out _))
        {
            _logger.LogWarning("[Barcode] Gecersiz EAN-13 verisi: {Data}", data);
            // Fallback: Code128 olarak uret
            return GenerateCode128Png(data, width, height);
        }

        _logger.LogDebug("[Barcode] EAN-13 PNG uretiliyor: {Data}", digits);

        var barPattern = EncodeEan13(digits);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(new PageSize(width, height + 20, Unit.Point));
                page.Margin(0);
                page.Content().Column(col =>
                {
                    col.Item().Height(height).Element(c => DrawBarcodePattern(c, barPattern, height));
                    col.Item().Height(20).AlignCenter()
                        .Text(digits).FontSize(10).FontFamily("Courier New");
                });
            });
        });

        return document.GeneratePdf();
    }

    /// <inheritdoc />
    public string GenerateCode128Svg(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);

        _logger.LogDebug("[Barcode] Code128 SVG uretiliyor: {Data}", data);

        var barPattern = EncodeCode128B(data);
        return RenderSvg(barPattern, data, width: 400, height: 80);
    }

    // ══════════════════════════════════════════════════
    //  Code128-B Encoding
    // ══════════════════════════════════════════════════

    /// <summary>
    /// Code128 Subset B bar/space pattern dizisi uretir.
    /// Her karakter 6 elemandan olusur (bar, space, bar, space, bar, space).
    /// </summary>
    private static bool[] EncodeCode128B(string data)
    {
        var values = new List<int>();

        // Start Code B = 104
        values.Add(104);

        foreach (var ch in data)
        {
            var value = ch - 32;
            if (value is < 0 or > 95)
                value = 0; // Desteklenmeyen karakter → space
            values.Add(value);
        }

        // Checksum
        var checksum = values[0];
        for (int i = 1; i < values.Count; i++)
            checksum += values[i] * i;
        checksum %= 103;
        values.Add(checksum);

        // Stop = 106
        values.Add(106);

        // Her value'yu bar pattern'e donustur
        var bits = new List<bool>();
        foreach (var val in values)
        {
            var pattern = Code128Patterns[val];
            foreach (var bit in pattern)
                bits.Add(bit);
        }

        // Termination bar (2 unit)
        bits.Add(true);
        bits.Add(true);

        return bits.ToArray();
    }

    // ══════════════════════════════════════════════════
    //  EAN-13 Encoding
    // ══════════════════════════════════════════════════

    private static bool[] EncodeEan13(string digits)
    {
        var bits = new List<bool>();
        var firstDigit = digits[0] - '0';
        var parityPattern = Ean13FirstDigitParity[firstDigit];

        // Start guard: 101
        bits.AddRange(new[] { true, false, true });

        // Left 6 digits
        for (int i = 0; i < 6; i++)
        {
            var digit = digits[i + 1] - '0';
            var encoding = parityPattern[i] == 'O'
                ? Ean13OddEncoding[digit]
                : Ean13EvenEncoding[digit];
            foreach (var b in encoding)
                bits.Add(b);
        }

        // Center guard: 01010
        bits.AddRange(new[] { false, true, false, true, false });

        // Right 6 digits
        for (int i = 0; i < 6; i++)
        {
            var digit = digits[i + 7] - '0';
            var encoding = Ean13RightEncoding[digit];
            foreach (var b in encoding)
                bits.Add(b);
        }

        // End guard: 101
        bits.AddRange(new[] { true, false, true });

        return bits.ToArray();
    }

    private static int CalculateEan13CheckDigit(string first12)
    {
        var sum = 0;
        for (int i = 0; i < 12; i++)
        {
            var d = first12[i] - '0';
            sum += (i % 2 == 0) ? d : d * 3;
        }
        var check = (10 - (sum % 10)) % 10;
        return check;
    }

    // ══════════════════════════════════════════════════
    //  QuestPDF Drawing
    // ══════════════════════════════════════════════════

    /// <summary>
    /// QuestPDF Row layout ile bar pattern cizer.
    /// true = siyah bar, false = beyaz bosluk.
    /// Her bit 1 birimlik kolon olarak cizilir.
    /// </summary>
    private static void DrawBarcodePattern(IContainer container, bool[] pattern, int height)
    {
        container.Row(row =>
        {
            foreach (var bit in pattern)
            {
                row.RelativeItem(1).Height(height)
                    .Background(bit ? Colors.Black : Colors.White);
            }
        });
    }

    // ══════════════════════════════════════════════════
    //  SVG Rendering
    // ══════════════════════════════════════════════════

    private static string RenderSvg(bool[] pattern, string data, int width, int height)
    {
        var barWidth = (double)width / pattern.Length;
        var sb = new StringBuilder();

        sb.AppendLine(CultureInfo.InvariantCulture,
            $"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height + 20}" viewBox="0 0 {width} {height + 20}">""");
        sb.AppendLine($"""  <rect width="{width}" height="{height + 20}" fill="white"/>""");

        for (int i = 0; i < pattern.Length; i++)
        {
            if (!pattern[i]) continue;

            var x = (i * barWidth).ToString("F2", CultureInfo.InvariantCulture);
            var w = barWidth.ToString("F2", CultureInfo.InvariantCulture);
            sb.AppendLine(
                $"""  <rect x="{x}" y="0" width="{w}" height="{height}" fill="black"/>""");
        }

        // Metin altta
        var textX = (width / 2).ToString(CultureInfo.InvariantCulture);
        sb.AppendLine(
            $"""  <text x="{textX}" y="{height + 15}" text-anchor="middle" font-family="Courier New" font-size="12">{data}</text>""");
        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    // ══════════════════════════════════════════════════
    //  Code128 Pattern Lookup Table
    // ══════════════════════════════════════════════════

    // Her deger: 11 birimlik bar/space pattern (true=bar, false=space)
    private static readonly bool[][] Code128Patterns =
    [
        // 0-9
        [true,true,false,true,true,false,false,true,true,false,false],   // 0: space
        [true,true,false,false,true,true,false,true,true,false,false],   // 1: !
        [true,true,false,false,true,true,false,false,true,true,false],   // 2: "
        [true,false,false,true,false,false,true,true,false,false,false], // 3: #
        [true,false,false,true,false,false,false,true,true,false,false], // 4: $
        [true,false,false,false,true,false,false,true,true,false,false], // 5: %
        [true,false,false,true,true,false,false,true,false,false,false], // 6: &
        [true,false,false,true,true,false,false,false,true,false,false], // 7: '
        [true,false,false,false,true,true,false,false,true,false,false], // 8: (
        [true,true,false,false,true,false,false,true,false,false,false], // 9: )
        // 10-19
        [true,true,false,false,true,false,false,false,true,false,false], // 10: *
        [true,true,false,false,false,true,false,false,true,false,false], // 11: +
        [true,false,true,true,false,false,true,true,true,false,false],   // 12: ,
        [true,false,false,true,true,false,true,true,true,false,false],   // 13: -
        [true,false,false,true,true,false,false,true,true,true,false],   // 14: .
        [true,false,true,true,true,false,false,true,true,false,false],   // 15: /
        [true,false,false,true,true,true,false,true,true,false,false],   // 16: 0
        [true,false,false,true,true,true,false,false,true,true,false],   // 17: 1
        [true,true,false,false,true,false,true,true,true,false,false],   // 18: 2
        [true,true,false,false,true,false,false,true,true,true,false],   // 19: 3
        // 20-29
        [true,true,false,false,false,true,false,true,true,true,false],   // 20: 4
        [true,true,false,true,true,true,false,false,true,false,false],   // 21: 5
        [true,true,false,false,true,true,true,false,true,false,false],   // 22: 6
        [true,true,true,false,true,true,false,true,true,true,false],     // 23: 7
        [true,true,true,false,true,false,false,true,true,false,false],   // 24: 8
        [true,true,true,false,false,true,false,true,true,false,false],   // 25: 9
        [true,true,true,false,false,true,false,false,true,true,false],   // 26: :
        [true,true,true,false,true,true,false,false,true,false,false],   // 27: ;
        [true,true,true,false,false,true,true,false,true,false,false],   // 28: <
        [true,true,true,false,false,true,true,false,false,true,false],   // 29: =
        // 30-39
        [true,true,false,true,true,false,true,true,false,false,false],   // 30: >
        [true,true,false,true,true,false,false,false,true,true,false],   // 31: ?
        [true,true,false,false,true,true,false,true,true,false,false],   // 32: @  (ASCII 64, value 32)
        [true,false,false,false,false,true,false,false,true,false,false], // 33: A
        [true,false,false,true,false,false,false,false,true,false,false], // 34: B
        [false,false,true,false,false,true,false,false,false,false,false],// 35: C
        [false,false,true,false,false,false,false,true,false,false,false],// 36: D
        [false,false,false,false,true,false,false,true,false,false,false],// 37: E
        [false,false,true,false,true,false,false,false,false,false,false],// 38: F
        [false,false,true,false,false,false,true,false,false,false,false],// 39: G
        // 40-49
        [false,false,false,false,true,false,true,false,false,false,false],// 40: H
        [true,false,false,true,false,true,true,true,false,false,false],   // 41: I
        [true,false,false,true,false,false,false,true,true,true,false],   // 42: J
        [true,false,false,false,true,false,true,true,true,false,false],   // 43: K
        [true,false,false,false,true,false,false,true,true,true,false],   // 44: L
        [true,false,false,false,false,true,false,true,true,true,false],   // 45: M
        [true,false,false,false,false,true,false,false,true,true,true],   // 46: N
        [true,true,true,false,true,false,false,false,true,false,false],   // 47: O
        [true,false,false,true,true,true,false,true,false,false,false],   // 48: P
        [true,false,false,false,true,true,true,false,true,false,false],   // 49: Q
        // 50-59
        [true,false,false,true,true,true,false,false,false,true,false],   // 50: R
        [true,true,true,false,false,false,true,false,true,false,false],   // 51: S
        [true,true,true,false,false,false,true,false,false,true,false],   // 52: T
        [true,true,true,false,false,true,false,false,false,true,false],   // 53: U
        [true,true,false,true,true,true,false,true,false,false,false],   // 54: V
        [true,true,false,false,true,true,true,false,true,false,false],   // 55: W
        [true,true,false,false,false,true,true,true,false,true,false],   // 56: X
        [true,false,true,false,false,false,true,true,false,false,false], // 57: Y
        [true,false,false,false,true,false,true,true,false,false,false], // 58: Z
        [true,false,false,false,true,false,false,false,true,true,false], // 59: [
        // 60-69
        [true,false,true,false,false,false,false,true,true,false,false], // 60: \
        [true,false,false,false,true,false,false,false,false,true,true], // 61: ]
        [true,true,false,false,false,true,false,true,false,false,false], // 62: ^
        [true,true,false,false,false,true,false,false,false,true,false], // 63: _
        [true,false,true,true,false,false,false,false,true,false,false], // 64: `
        [true,false,false,true,true,false,false,false,false,true,false], // 65: a
        [true,false,false,false,false,true,true,false,false,true,false], // 66: b
        [true,false,true,true,false,false,true,false,false,false,false], // 67: c
        [true,false,true,true,false,false,false,true,false,false,false], // 68: d
        [true,false,false,true,true,false,true,false,false,false,false], // 69: e
        // 70-79
        [true,false,false,true,true,false,false,false,true,false,false], // 70: f
        [true,false,false,false,true,true,false,true,false,false,false], // 71: g
        [true,false,false,false,true,true,false,false,false,true,false], // 72: h
        [true,true,false,true,false,false,true,false,false,false,false], // 73: i
        [true,true,false,false,true,false,true,false,false,false,false], // 74: j
        [true,true,false,false,false,true,false,true,false,false,false], // 75: k
        [true,false,true,false,false,true,true,false,false,false,false], // 76: l
        [true,false,false,true,false,true,true,false,false,false,false], // 77: m
        [true,false,false,false,true,false,true,false,false,true,false], // 78: n (not used often)
        [true,true,false,true,false,false,false,true,false,false,false], // 79: o
        // 80-89
        [true,true,false,false,true,false,false,false,true,false,false], // 80: p
        [true,true,false,false,false,true,false,false,true,false,false], // 81: q
        [true,false,true,true,false,true,true,true,false,false,false],   // 82: r
        [true,false,true,true,false,false,false,true,true,true,false],   // 83: s
        [true,false,false,false,true,true,false,true,true,true,false],   // 84: t
        [true,false,true,true,true,false,true,true,false,false,false],   // 85: u
        [true,false,true,true,true,false,false,false,true,true,false],   // 86: v
        [true,false,false,false,true,true,true,false,true,true,false],   // 87: w
        [true,true,true,false,true,false,true,true,true,false,false],   // 88: x
        [true,true,true,false,false,true,false,true,true,true,false],   // 89: y
        // 90-99
        [true,true,true,false,false,false,true,false,true,true,false],   // 90: z
        [true,true,true,false,true,true,true,false,true,false,false],   // 91: {
        [true,true,true,false,false,true,true,true,false,true,false],   // 92: |
        [true,true,false,true,false,false,false,false,true,true,false], // 93: }
        [true,true,false,false,true,false,false,false,false,true,true], // 94: ~
        [true,true,false,false,false,true,true,false,false,false,true], // 95: DEL
        [true,true,false,true,true,true,true,false,true,false,false],   // 96: FNC3
        [true,true,false,false,true,true,true,true,false,true,false],   // 97: FNC2
        [true,true,true,false,true,false,false,false,false,true,false], // 98: SHIFT
        [true,false,false,false,true,true,true,true,false,true,false],  // 99: CODE C
        // 100-106
        [true,false,true,true,true,true,false,false,false,true,false],  // 100: CODE B
        [true,false,false,true,true,true,true,false,true,false,false],  // 101: FNC4
        [true,false,false,true,false,true,true,true,true,false,false],  // 102: FNC1
        [true,true,false,true,false,false,true,true,true,true,false],   // 103: START A
        [true,true,false,true,false,false,true,true,true,true,false],   // 104: START B
        [true,true,false,true,false,false,true,true,true,true,false],   // 105: START C
        [true,true,false,false,false,true,true,true,false,true,false],  // 106: STOP
    ];

    // ══════════════════════════════════════════════════
    //  EAN-13 Lookup Tables
    // ══════════════════════════════════════════════════

    private static readonly string[] Ean13FirstDigitParity =
    [
        "OOOOOO", // 0
        "OOEOEE", // 1
        "OOEEOE", // 2
        "OOEEEO", // 3
        "OEOOEE", // 4
        "OEEOOE", // 5
        "OEEEOO", // 6
        "OEOEOE", // 7
        "OEOEEO", // 8
        "OEEOEO", // 9
    ];

    // Odd parity (L-code)
    private static readonly bool[][] Ean13OddEncoding =
    [
        [false,false,false,true,true,false,true], // 0
        [false,false,true,true,false,false,true], // 1
        [false,false,true,false,false,true,true], // 2
        [false,true,true,true,true,false,true],   // 3
        [false,true,false,false,false,true,true], // 4
        [false,true,true,false,false,false,true], // 5
        [false,true,false,true,true,true,true],   // 6
        [false,true,true,true,false,true,true],   // 7
        [false,true,true,false,true,true,true],   // 8
        [false,false,false,true,false,true,true],  // 9
    ];

    // Even parity (G-code)
    private static readonly bool[][] Ean13EvenEncoding =
    [
        [false,true,false,false,true,true,true], // 0
        [false,true,true,false,false,true,true], // 1
        [false,false,true,true,false,true,true], // 2
        [false,true,false,false,false,false,true],// 3
        [false,false,true,true,true,false,true], // 4
        [false,true,true,true,false,false,true], // 5
        [false,false,false,false,true,false,true],// 6
        [false,false,true,false,false,false,true],// 7
        [false,false,false,true,false,false,true],// 8
        [false,false,true,false,true,true,true], // 9
    ];

    // Right-hand (R-code)
    private static readonly bool[][] Ean13RightEncoding =
    [
        [true,true,true,false,false,true,false], // 0
        [true,true,false,false,true,true,false], // 1
        [true,true,false,true,true,false,false], // 2
        [true,false,false,false,false,true,false],// 3
        [true,false,true,true,true,false,false], // 4
        [true,false,false,true,true,true,false], // 5
        [true,false,true,false,false,false,false],// 6
        [true,false,false,false,true,false,false],// 7
        [true,false,false,true,false,false,false],// 8
        [true,true,true,false,true,false,false], // 9
    ];
}
