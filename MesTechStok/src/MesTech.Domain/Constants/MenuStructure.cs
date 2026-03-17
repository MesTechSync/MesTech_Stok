namespace MesTech.Domain.Constants;

/// <summary>
/// 4 platformda (WPF, Blazor, HTML, Avalonia) kullanilan standart menu yapisi.
/// Grup adlari ve siralama DEGISMEZ. Ikon ve rota platform-specific olabilir.
/// EMR: ENT-M4-MENU-v2
/// </summary>
public static class MenuStructure
{
    public static readonly IReadOnlyList<MenuGroup> Groups = new List<MenuGroup>
    {
        new(1, "Dashboard", "fas fa-tachometer-alt", "PackIconMaterial.ViewDashboard",
            new[] { "Genel Bakis" }),

        new(2, "Urunler", "fas fa-box", "PackIconMaterial.Package",
            new[] { "Urun Listesi", "Urun Ekle", "Toplu Ice Aktar", "Varyantlar", "Fiyat Guncelle" }),

        new(3, "Siparisler", "fas fa-shopping-cart", "PackIconMaterial.Cart",
            new[] { "Siparis Listesi", "Siparis Detay", "Platform Bazli" }),

        new(4, "Stok", "fas fa-warehouse", "PackIconMaterial.Warehouse",
            new[] { "Envanter", "Lot Takip", "Depo Yerlesim", "Stok Guncelle" }),

        new(5, "Kategoriler", "fas fa-layer-group", "PackIconMaterial.Layers",
            new[] { "Kategori Agaci", "Platform Esleme" }),

        new(6, "Kargo", "fas fa-truck", "PackIconMaterial.Truck",
            new[] { "Gonderim", "Takip", "Etiket Yazdir", "Otomatik Atama" }),

        new(7, "E-Fatura", "fas fa-file-invoice", "PackIconMaterial.FileDocument",
            new[] { "Fatura Listesi", "Fatura Olustur", "Fatura Detay" }),

        new(8, "Finans", "fas fa-coins", "PackIconMaterial.Cash",
            new[] { "Gelir-Gider", "Cari Hesaplar", "Teklifler", "Mutabakat", "Banka", "KDV Hesaplama" }),

        new(9, "CRM", "fas fa-users", "PackIconMaterial.AccountGroup",
            new[] { "Musteriler", "Firsatlar", "Adaylar", "Pipeline" }),

        new(10, "Dropshipping", "fas fa-truck-loading", "PackIconMaterial.TruckDelivery",
            new[] { "Dashboard", "Urun Havuzu", "Tedarikciler", "Ice Aktar" }),

        new(11, "Raporlar", "fas fa-chart-bar", "PackIconMaterial.ChartBar",
            new[] { "Satis Raporu", "Stok Raporu", "Kargo Raporu", "Muhasebe Raporu", "Platform Raporu" }),

        new(12, "Ayarlar", "fas fa-cog", "PackIconMaterial.Cog",
            new[] { "Profil", "Platform Baglanti", "Depo Ayarlari", "Bildirimler", "Sistem" })
    };

}
