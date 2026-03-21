using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;

namespace MesTech.Avalonia.Dialogs;

public partial class CommandPaletteDialog : Window
{
    private readonly List<CommandItem> _allCommands;
    public CommandItem? SelectedCommand { get; private set; }

    public CommandPaletteDialog()
    {
        InitializeComponent();
        _allCommands = BuildCommandRegistry();
        ResultsList.ItemsSource = _allCommands;

        Opened += (_, _) =>
        {
            QueryBox.Focus();
            QueryBox.SelectAll();
        };
    }

    private void OnQueryChanged(object? sender, TextChangedEventArgs e)
    {
        var query = QueryBox.Text?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(query))
        {
            ResultsList.ItemsSource = _allCommands;
            return;
        }

        var filtered = _allCommands
            .Where(c => c.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || c.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (c.Keywords?.Any(k => k.Contains(query, StringComparison.OrdinalIgnoreCase)) ?? false))
            .ToList();

        ResultsList.ItemsSource = filtered;
        if (filtered.Count > 0)
            ResultsList.SelectedIndex = 0;
    }

    private void OnQueryKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (ResultsList.ItemCount > 0)
                {
                    ResultsList.SelectedIndex = Math.Min(ResultsList.SelectedIndex + 1, ResultsList.ItemCount - 1);
                    e.Handled = true;
                }
                break;
            case Key.Up:
                if (ResultsList.ItemCount > 0)
                {
                    ResultsList.SelectedIndex = Math.Max(ResultsList.SelectedIndex - 1, 0);
                    e.Handled = true;
                }
                break;
            case Key.Enter:
                ConfirmSelection();
                e.Handled = true;
                break;
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Double-click support handled by selection
    }

    private void ConfirmSelection()
    {
        SelectedCommand = ResultsList.SelectedItem as CommandItem;
        if (SelectedCommand != null)
            Close();
    }

    private static List<CommandItem> BuildCommandRegistry()
    {
        return
        [
            // Navigasyon
            new("Ana Sayfa", "Navigasyon", "\U0001F3E0", "Ctrl+1", ["dashboard", "anasayfa"]),
            new("Siparisler", "Navigasyon", "\U0001F4E6", "Ctrl+2", ["orders", "siparis"]),
            new("Urunler", "Navigasyon", "\U0001F4CB", "Ctrl+3", ["products", "urun", "stok"]),
            new("Muhasebe", "Navigasyon", "\U0001F4B0", "Ctrl+4", ["accounting", "muhasebe", "finans"]),
            new("CRM", "Navigasyon", "\U0001F465", "Ctrl+5", ["crm", "musteri"]),
            new("E-Fatura", "Navigasyon", "\U0001F4C4", "Ctrl+6", ["invoice", "fatura"]),
            new("Kargo", "Navigasyon", "\U0001F69A", "Ctrl+7", ["shipping", "kargo"]),
            new("Raporlar", "Navigasyon", "\U0001F4CA", "Ctrl+8", ["reports", "rapor"]),
            new("Ayarlar", "Navigasyon", "\U00002699\U0000FE0F", "Ctrl+9", ["settings", "ayar"]),

            // Finans
            new("Kar/Zarar Raporu", "Finans", "\U0001F4C8", "", ["profit", "loss", "kar", "zarar"]),
            new("Nakit Akisi", "Finans", "\U0001F4B5", "", ["cash", "flow", "nakit"]),
            new("Butce Ozeti", "Finans", "\U0001F4CA", "", ["budget", "butce"]),
            new("KDV Raporu", "Finans", "\U0001F4CB", "", ["kdv", "vat", "vergi"]),
            new("Komisyon Takibi", "Finans", "\U0001F4B3", "", ["commission", "komisyon"]),
            new("Mutabakat", "Finans", "\U00002696\U0000FE0F", "", ["reconciliation", "mutabakat"]),
            new("Sabit Giderler", "Finans", "\U0001F4C5", "", ["fixed", "expense", "sabit", "gider"]),
            new("Bordro", "Finans", "\U0001F4B4", "", ["salary", "bordro", "maas"]),

            // Platformlar
            new("Trendyol Paneli", "Platform", "\U0001F6D2", "", ["trendyol"]),
            new("Hepsiburada Paneli", "Platform", "\U0001F6D2", "", ["hepsiburada", "hb"]),
            new("N11 Paneli", "Platform", "\U0001F6D2", "", ["n11"]),
            new("Amazon Paneli", "Platform", "\U0001F6D2", "", ["amazon"]),
            new("Ciceksepeti Paneli", "Platform", "\U0001F338", "", ["ciceksepeti", "cs"]),
            new("eBay Paneli", "Platform", "\U0001F6D2", "", ["ebay"]),

            // Islemler
            new("Yeni Siparis", "Islem", "\U00002795", "", ["new", "order", "siparis"]),
            new("Stok Guncelle", "Islem", "\U0001F504", "", ["stock", "update", "stok"]),
            new("Toplu Fatura", "Islem", "\U0001F4C4", "", ["bulk", "invoice", "toplu", "fatura"]),
            new("Platform Senkronizasyon", "Islem", "\U0001F504", "", ["sync", "senkron"]),
            new("Buybox Takibi", "Islem", "\U0001F3AF", "", ["buybox"]),

            // Sistem
            new("Tam Ekran", "Sistem", "\U000026F6", "F11", ["fullscreen", "tam"]),
            new("Yenile", "Sistem", "\U0001F504", "F5", ["refresh", "yenile"]),
            new("Kilitle", "Sistem", "\U0001F512", "Ctrl+L", ["lock", "kilitle"]),
            new("Sidebar Ac/Kapa", "Sistem", "\U00002630", "Ctrl+B", ["sidebar", "menu"]),
        ];
    }
}

public class CommandItem
{
    public string Title { get; set; }
    public string Category { get; set; }
    public string Icon { get; set; }
    public string Shortcut { get; set; }
    public List<string>? Keywords { get; set; }

    public CommandItem(string title, string category, string icon, string shortcut, List<string>? keywords = null)
    {
        Title = title;
        Category = category;
        Icon = icon;
        Shortcut = shortcut;
        Keywords = keywords;
    }
}
