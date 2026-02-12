using System;

namespace MesTechStok.Desktop.Utils
{
    public static class EventBus
    {
        public static event Action<string?>? ProductsChanged;
        public static event Action<string?>? CompanySettingsChanged;

        public static void PublishProductsChanged(string? barcode)
        {
            try { ProductsChanged?.Invoke(barcode); } catch { }
        }

        public static void PublishCompanySettingsChanged(string? companyName)
        {
            try { CompanySettingsChanged?.Invoke(companyName); } catch { }
        }
    }
}


