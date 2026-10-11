namespace CoffeeNChill.Functions.Models
{
    public class OrderRequest
    {
        public string? OrderId { get; set; }
        public string? CustomerName { get; set; }
        public string[]? SelectedItemSKUs { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}