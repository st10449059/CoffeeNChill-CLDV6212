// Code Attribution:
// The ITableEntity implementation for Azure Table Storage was adapted from 
// the official Microsoft Azure Data Tables documentation.
// Reference: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.itableentity

using Azure;
using Azure.Data.Tables;
using System;

namespace CoffeeNChill.Functions.Models
{
    public class MenuItem : ITableEntity
    {
        // Required by Azure Table Storage
        public string PartitionKey { get; set; } // Used for Category (e.g., "Hot Drinks")
        public string RowKey { get; set; }       // Used for Unique Item ID (e.g., "COF-001")
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Specific properties required by the CoffeeNChill POE scenario
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}