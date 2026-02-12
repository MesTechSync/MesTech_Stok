namespace MesTechStok.Core.Integrations.OpenCart.Dtos
{
    public class OpenCartPriceUpdate
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SpecialPrice { get; set; }
        public DateTime? SpecialStartDate { get; set; }
        public DateTime? SpecialEndDate { get; set; }
    }
}
