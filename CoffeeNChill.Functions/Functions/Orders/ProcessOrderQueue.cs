using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Orders;

public class ProcessOrderQueue
{
    private readonly OrdersTableService _orders;
    private readonly ILogger<ProcessOrderQueue> _logger;
    public ProcessOrderQueue(OrdersTableService orders, ILogger<ProcessOrderQueue> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    [Function("ProcessOrderQueue")]
    public async Task Run(
        [QueueTrigger("order-processing-queue", Connection = "AzureWebJobsStorage")] string body)
    {
        try
        {
            var message = JsonSerializer.Deserialize<OrderMessage>(body);
            if (message == null || string.IsNullOrWhiteSpace(message.OrderId) ||
                message.OrderId.Length > 128 ||
                message.OrderId.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_') ||
                string.IsNullOrWhiteSpace(message.CustomerName) ||
                message.SelectedItemSKUs == null || message.SelectedItemSKUs.Length == 0 ||
                message.SelectedItemSKUs.Any(string.IsNullOrWhiteSpace) ||
                message.TotalPrice <= 0 || message.TotalPrice > 1000000 ||
                decimal.Round(message.TotalPrice, 2) != message.TotalPrice ||
                message.OrderTimestamp == default)
                throw new InvalidOperationException("Invalid order queue message.");

            var date = message.OrderTimestamp.UtcDateTime.ToString("yyyy-MM-dd");
            var existing = await _orders.GetAsync(date, message.OrderId);
            if (existing == null)
                await _orders.CreateAsync(new OrderEntity
                {
                    PartitionKey = date,
                    RowKey = message.OrderId,
                    CustomerName = message.CustomerName,
                    SelectedItemSKUsJson = JsonSerializer.Serialize(message.SelectedItemSKUs),
                    TotalPriceCents = checked((long)(message.TotalPrice * 100)),
                    OrderTimestamp = message.OrderTimestamp,
                    Status = "Received",
                    ReceivedAt = DateTimeOffset.UtcNow
                });

            string[] stages = { "Received", "Preparing", "Ready", "Collected" };
            while (true)
            {
                var order = await _orders.GetAsync(date, message.OrderId)
                    ?? throw new InvalidOperationException("Stored order was not found.");
                var index = Array.IndexOf(stages, order.Status);
                if (index < 0) throw new InvalidOperationException("Unknown order status.");
                if (index == 3) return;
                await Task.Delay(TimeSpan.FromSeconds(5));
                order.Status = stages[index + 1];
                var now = DateTimeOffset.UtcNow;
                if (order.Status == "Preparing") order.PreparingAt = now;
                if (order.Status == "Ready") order.ReadyAt = now;
                if (order.Status == "Collected") order.CollectedAt = now;
                await _orders.UpdateAsync(order);
                _logger.LogInformation("Order {OrderId}: {Status}", order.RowKey, order.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order processing failed; the message will be retried.");
            throw;
        }
    }
}