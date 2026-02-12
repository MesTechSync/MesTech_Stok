using System;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;

namespace MesTechStok.Desktop
{
    /// <summary>
    /// Widget test window
    /// </summary>
    public partial class TestWidgetWindow : Window
    {
        // Windows API - Pencereyi zorla öne getirmek için
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        public TestWidgetWindow()
        {
            try
            {
                InitializeComponent();

                Title = "🧪 MesTech Widget Test - GÖRÜNÜR OLDU!";
                WindowState = WindowState.Maximized;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // Pencereyi ZORLA görünür yap
                this.Show();
                this.Activate();
                this.Topmost = true;

                // Windows API ile zorla öne getir
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                ShowWindow(helper.Handle, SW_RESTORE);
                ShowWindow(helper.Handle, SW_SHOW);
                SetForegroundWindow(helper.Handle);

                // Ekranda mesaj göster
                MessageBox.Show("🎉 Widget Test Penceresi Açıldı!\n\n" +
                               "✅ Sol alt köşede flu saat\n" +
                               "✅ Sağ tarafta sıcaklık sistemi\n" +
                               "✅ Canlı arka plan",
                               "MesTech Widget Test",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

                // 3 saniye sonra Topmost'u kapat
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) =>
                {
                    this.Topmost = false;
                    timer.Stop();
                };
                timer.Start();

#if DEBUG
                Console.WriteLine("✅ TestWidgetWindow ZORLA görünür hale getirildi!");
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"❌ TestWidgetWindow hatası: {ex.Message}");
#endif
                MessageBox.Show($"Widget penceresi yüklenemedi: {ex.Message}", "Hata");
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Pencereyi tamamen öne getir
            this.Topmost = true;
            this.Topmost = false; // Hemen kapat ama öne gelmiş olsun
        }
    }
}