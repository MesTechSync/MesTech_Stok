using System;

namespace MesTechStok.Desktop.Services
{
    public class ProductQuickFilters
    {
        public bool OutOfStockOnly { get; set; }
        public bool LowStockOnly { get; set; }
        public bool DiscountedOnly { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }
}


