using System;

namespace CoffeeNChill.Functions.Models
{
    public class OrderMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string[] SelectedItemSKUs { get; set; } = Array.Empty<string>();
        public decimal TotalPrice { get; set; }
        public DateTimeOffset OrderTimestamp { get; set; }
    }
}
