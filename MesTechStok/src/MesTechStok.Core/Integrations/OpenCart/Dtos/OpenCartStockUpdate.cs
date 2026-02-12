using System;

namespace MesTechStok.Core.Integrations.OpenCart.Dtos
{
    public class OpenCartStockUpdate
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool InStock { get; set; }
        public DateTime UpdateDate { get; set; } = DateTime.Now;
        public string UpdateReason { get; set; } = string.Empty;
    }
}
