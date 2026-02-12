using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MesTechStok.Desktop.Views;

namespace MesTechStok.Desktop.Services
{
    public static class ProductUploadWindowManager
    {
        private const int MaxWindows = 10;
        private static readonly List<ProductUploadPopup> OpenWindows = new();

        public static bool TryOpen(Window? owner = null)
        {
            Cleanup();
            if (OpenWindows.Count >= MaxWindows)
            {
                MesTechStok.Desktop.Utils.ToastManager.ShowWarning("En fazla 10 yükleme penceresi açılabilir.", "Ürün Yükleme");
                return false;
            }
            try
            {
                var w = new ProductUploadPopup();
                if (owner != null) w.Owner = owner;
                w.Closed += (_, __) => Cleanup();
                OpenWindows.Add(w);
                w.Show();
                try { w.Activate(); w.Focus(); } catch { }
                return true;
            }
            catch (Exception ex)
            {
                MesTechStok.Desktop.Utils.ToastManager.ShowError($"Popup hatası: {ex.Message}", "Ürün Yükleme");
                return false;
            }
        }

        // Mevcut ürünle (düzenleme veya ön-dolum) açma
        public static bool TryOpen(Window? owner, MesTechStok.Desktop.Models.ProductItem item)
        {
            Cleanup();
            if (OpenWindows.Count >= MaxWindows)
            {
                MesTechStok.Desktop.Utils.ToastManager.ShowWarning("En fazla 10 yükleme penceresi açılabilir.", "Ürün Yükleme");
                return false;
            }
            try
            {
                var w = new ProductUploadPopup(item);
                if (owner != null) w.Owner = owner;
                w.Closed += (_, __) => Cleanup();
                OpenWindows.Add(w);
                w.Show();
                try { w.Activate(); w.Focus(); } catch { }
                return true;
            }
            catch (Exception ex)
            {
                MesTechStok.Desktop.Utils.ToastManager.ShowError($"Popup hatası: {ex.Message}", "Ürün Yükleme");
                return false;
            }
        }

        private static void Cleanup()
        {
            for (int i = OpenWindows.Count - 1; i >= 0; i--)
            {
                if (!OpenWindows[i].IsVisible) OpenWindows.RemoveAt(i);
            }
        }

        public static int Count => OpenWindows.Count(w => w.IsVisible);

        // Barkodla yeni ürün açma (ön-dolu)
        public static bool TryOpenWithBarcode(Window? owner, string barcode)
        {
            Cleanup();
            if (OpenWindows.Count >= MaxWindows)
            {
                MesTechStok.Desktop.Utils.ToastManager.ShowWarning("En fazla 10 yükleme penceresi açılabilir.", "Ürün Yükleme");
                return false;
            }
            try
            {
                var w = new ProductUploadPopup(barcode);
                if (owner != null) w.Owner = owner;
                w.Closed += (_, __) => Cleanup();
                OpenWindows.Add(w);
                w.Show();
                try { w.Activate(); w.Focus(); } catch { }
                return true;
            }
            catch (Exception ex)
            {
                MesTechStok.Desktop.Utils.ToastManager.ShowError($"Popup hatası: {ex.Message}", "Ürün Yükleme");
                return false;
            }
        }
    }
}


