using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;
using Azure;

namespace CoffeeNChill.Functions.Services
{
    public enum OrderStatusUpdateResult
    {
        Updated,
        AlreadyCurrent,
        NotFound,
        InvalidTransition,
        ConcurrentConflict
    }
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
        public async Task<OrderStatusUpdateResult> TryAdvanceStatusAsync(
            string orderDate,
            string orderId,
            string newStatus)
                {
                    string[] stages =
                    {
                "Received",
                "Preparing",
                "Ready",
                "Collected"
            };

                    // Read the latest order and its ETag.
                    var order = await GetAsync(orderDate, orderId);

                    if (order == null)
                        return OrderStatusUpdateResult.NotFound;

                    int currentIndex = Array.IndexOf(stages, order.Status);
                    int requestedIndex = Array.IndexOf(stages, newStatus);

                    // Reject unknown statuses and invalid stored states.
                    if (currentIndex < 0 || requestedIndex < 0)
                        return OrderStatusUpdateResult.InvalidTransition;

                    // Repeating the current status does not change the record.
                    if (currentIndex == requestedIndex)
                        return OrderStatusUpdateResult.AlreadyCurrent;

                    // Only allow progression by one stage.
                    if (requestedIndex != currentIndex + 1)
                        return OrderStatusUpdateResult.InvalidTransition;

                    var now = DateTimeOffset.UtcNow;

                    order.Status = newStatus;

                    // Preserve any timestamps already recorded.
                    switch (newStatus)
                    {
                        case "Preparing":
                            order.PreparingAt ??= now;
                            break;

                        case "Ready":
                            order.ReadyAt ??= now;
                            break;

                        case "Collected":
                            order.CollectedAt ??= now;
                            break;
                    }

                    try
                    {
                        // UpdateAsync uses the entity's ETag.
                        await UpdateAsync(order);
                        return OrderStatusUpdateResult.Updated;
                    }
                    catch (RequestFailedException ex) when (ex.Status == 412)
                    {
                        // Another process changed the record first.
                        return OrderStatusUpdateResult.ConcurrentConflict;
                    }
                    catch (RequestFailedException ex) when (ex.Status == 404)
                    {
                        // The record disappeared before the update completed.
                        return OrderStatusUpdateResult.NotFound;
            }
        }
    }
}