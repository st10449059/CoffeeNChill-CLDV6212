using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChill.Functions.Services
{
    public class OrdersTableService
    {
        private readonly TableClient _table;

        public OrdersTableService(IConfiguration configuration)
        {
            var connection = configuration["AzureWebJobsStorage"];
            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException("AzureWebJobsStorage is not configured.");
            _table = new TableClient(connection, "Orders");
        }

        public async Task<OrderEntity?> GetAsync(string orderDate, string orderId)
        {
            await _table.CreateIfNotExistsAsync();
            var result = await _table.GetEntityIfExistsAsync<OrderEntity>(orderDate, orderId);
            return result.HasValue ? result.Value : null;
        }

        public async Task CreateAsync(OrderEntity order)
        {
            await _table.CreateIfNotExistsAsync();
            await _table.AddEntityAsync(order);
        }

        public async Task UpdateAsync(OrderEntity order)
        {
            await _table.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);
        }
    }
}