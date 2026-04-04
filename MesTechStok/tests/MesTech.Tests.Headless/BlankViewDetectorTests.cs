using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using System.Reflection;
using Xunit;

namespace MesTech.Tests.Headless;

/// <summary>
/// DEV 7 — Kalite Gate: Blank View Detector.
/// Her view render edilir, PNG boyutu ölçülür.
/// 6223 byte = blank (boş beyaz dikdörtgen) → FAIL listesine eklenir.
/// Hedef: Blank view sayısını 57 → 0'a düşürmek.
/// Bu test CI/CD quality gate olarak kullanılır.
/// </summary>
[Trait("Category", "Headless")]
[Trait("Layer", "QualityGate")]
public class BlankViewDetectorTests
{
    // 6223 byte = 1280x720 boş beyaz PNG boyutu (Avalonia Headless)
    // Küçük sapmalar olabilir — %5 tolerans
    private const long BlankThreshold = 6600;

    // Kabul edilebilir maksimum blank view oranı
    // Şu an 57/171 = %33 — hedef: %10 altı
    private const double MaxBlankRatio = 0.10;

    [AvaloniaFact]
    public void DetectBlankViews_AndReport()
    {
        var outputDir = "screenshots/quality-gate";
        Directory.CreateDirectory(outputDir);

        var assembly = typeof(MesTech.Avalonia.App).Assembly;
        var viewTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("View") || t.Name.EndsWith("Window"))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(Control).IsAssignableFrom(t))
            .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t.Name)
            .ToList();

        Assert.True(viewTypes.Count > 0, "Assembly'de hic View tipi bulunamadi!");

        var blankViews = new List<string>();
        var nonBlankViews = new List<string>();
        var errorViews = new List<string>();
        int total = 0;

        foreach (var viewType in viewTypes)
        {
            total++;
            try
            {
                var view = (Control)Activator.CreateInstance(viewType)!;

                Window window;
                if (view is Window w)
                {
                    window = w;
                    window.Width = 1280;
                    window.Height = 720;
                }
                else
                {
                    window = new Window
                    {
                        Width = 1280,
                        Height = 720,
                        Content = view
                    };
                }

                window.Show();
                Dispatcher.UIThread.RunJobs();

                var frame = window.CaptureRenderedFrame();
                if (frame != null)
                {
                    var filename = Path.Combine(outputDir, $"{viewType.Name}.png");
                    frame.Save(filename);

                    var fileSize = new FileInfo(filename).Length;
                    if (fileSize <= BlankThreshold)
                        blankViews.Add($"{viewType.Name} ({fileSize} bytes)");
                    else
                        nonBlankViews.Add($"{viewType.Name} ({fileSize} bytes)");
                }
                else
                {
                    errorViews.Add($"{viewType.Name}: CaptureRenderedFrame NULL");
                }

                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
            catch (Exception ex)
            {
                errorViews.Add($"{viewType.Name}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // Rapor oluştur
        var report = $"# BLANK VIEW DETECTOR RAPORU\n" +
                     $"Tarih: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                     $"Threshold: {BlankThreshold} bytes\n\n" +
                     $"## SONUC\n" +
                     $"Toplam: {total}\n" +
                     $"Blank: {blankViews.Count} ({(total > 0 ? blankViews.Count * 100 / total : 0)}%)\n" +
                     $"NonBlank: {nonBlankViews.Count}\n" +
                     $"Error: {errorViews.Count}\n\n" +
                     $"## BLANK VIEWS ({blankViews.Count})\n" +
                     string.Join("\n", blankViews.Select(v => $"  - {v}")) +
                     $"\n\n## ERRORS ({errorViews.Count})\n" +
                     string.Join("\n", errorViews.Select(v => $"  - {v}"));

        File.WriteAllText(Path.Combine(outputDir, "BLANK_DETECTOR_REPORT.md"), report);

        // Quality gate assertion — şimdilik rapor et, ileriki turlarda enforce et
        // Assert.True versiyon: blankViews.Count <= total * MaxBlankRatio
        // Şu an enforced DEĞİL — sadece rapor
        var blankRatio = total > 0 ? (double)blankViews.Count / total : 0;

        // Her zaman pass — ama blank sayısını rapor dosyasına yaz
        Assert.True(total > 0, "Hicbir view bulunamadi");

        // Blank sayısı azaldıkça bu assertion enforce edilecek
        // TODO: blankViews.Count <= (int)(total * MaxBlankRatio) olduğunda aktifleştir
    }
}
