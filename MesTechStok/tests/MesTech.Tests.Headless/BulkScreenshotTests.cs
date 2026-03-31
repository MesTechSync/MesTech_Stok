using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using System.Reflection;
using Xunit;

namespace MesTech.Tests.Headless;

[Trait("Category", "Headless")]
[Trait("Layer", "UI")]
public class BulkScreenshotTests
{
    [AvaloniaFact]
    public void CaptureAllViews()
    {
        var viewsDir = "screenshots/views";
        Directory.CreateDirectory(viewsDir);

        // MesTech.Avalonia assembly'den tum View tiplerini bul
        var assembly = typeof(MesTech.Avalonia.App).Assembly;
        var viewTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("View") || t.Name.EndsWith("Window"))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(Control).IsAssignableFrom(t))
            .Where(t => t.GetConstructor(Type.EmptyTypes) != null) // parametresiz ctor zorunlu
            .OrderBy(t => t.Name)
            .ToList();

        Assert.True(viewTypes.Count > 0, "Assembly'de hic View tipi bulunamadi!");

        int captured = 0;
        int failed = 0;
        var errors = new List<string>();

        foreach (var viewType in viewTypes)
        {
            try
            {
                var view = (Control)Activator.CreateInstance(viewType)!;

                // Window tipleri direkt gosterilir, UserControl ise Window icine konur
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
                    var filename = Path.Combine(viewsDir, $"{viewType.Name}.png");
                    frame.Save(filename);
                    captured++;
                }
                else
                {
                    errors.Add($"{viewType.Name}: CaptureRenderedFrame NULL");
                    failed++;
                }

                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
            catch (Exception ex)
            {
                File.WriteAllText(
                    Path.Combine(viewsDir, $"{viewType.Name}_ERROR.txt"),
                    $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}"
                );
                errors.Add($"{viewType.Name}: {ex.GetType().Name} — {ex.Message}");
                failed++;
            }
        }

        // Rapor yaz
        var report = $"Toplam View Tipi: {viewTypes.Count}\n" +
                     $"Basarili Screenshot: {captured}\n" +
                     $"Basarisiz: {failed}\n" +
                     $"Basari Orani: {(viewTypes.Count > 0 ? (captured * 100 / viewTypes.Count) : 0)}%\n" +
                     $"Tarih: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";

        if (errors.Count > 0)
        {
            report += $"\nHATALAR:\n" + string.Join("\n", errors.Select(e => $"  - {e}"));
        }

        File.WriteAllText("screenshots/RAPOR.txt", report);

        // En az 1 screenshot alinmali
        Assert.True(captured > 0, $"Hicbir view screenshot alinamadi! {failed} hata olustu.");
    }
}
