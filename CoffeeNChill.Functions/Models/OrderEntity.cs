using System;
using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Functions.Models
{
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string SelectedItemSKUsJson { get; set; } = "[]";
        public long TotalPriceCents { get; set; }
        public DateTimeOffset OrderTimestamp { get; set; }
        public string Status { get; set; } = string.Empty;

        public DateTimeOffset? ReceivedAt { get; set; }
        public DateTimeOffset? PreparingAt { get; set; }
        public DateTimeOffset? ReadyAt { get; set; }
        public DateTimeOffset? CollectedAt { get; set; }
    }
}