using Azure;
using Azure.Data.Tables;
using System;

namespace CoffeeNChill.Functions.Models
{
    public class MenuItem : ITableEntity
    {
        // ITableEntity implementation required by Azure Table Storage
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties for CoffeeNChill menu items
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}