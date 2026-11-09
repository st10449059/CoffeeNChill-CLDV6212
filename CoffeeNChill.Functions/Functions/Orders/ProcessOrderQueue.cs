using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure;

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
                // Read the current order status.
                var order = await _orders.GetAsync(date, message.OrderId)
                    ?? throw new InvalidOperationException(
                        "Stored order was not found.");

                int index = Array.IndexOf(stages, order.Status);

                if (index < 0)
                    throw new InvalidOperationException("Unknown order status.");

                // Stop when the order has been collected.
                if (index == stages.Length - 1)
                    return;

                // Preserve the existing simulated preparation delay.
                await Task.Delay(TimeSpan.FromSeconds(5));

                // Re-read after the delay to detect Staff updates.
                order = await _orders.GetAsync(date, message.OrderId)
                    ?? throw new InvalidOperationException(
                        "Stored order was not found.");

                index = Array.IndexOf(stages, order.Status);

                if (index < 0)
                    throw new InvalidOperationException("Unknown order status.");

                if (index == stages.Length - 1)
                    return;

                string nextStatus = stages[index + 1];
                var now = DateTimeOffset.UtcNow;

                order.Status = nextStatus;

                // Preserve timestamps written by earlier updates.
                switch (nextStatus)
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
                    // UpdateAsync checks the entity's ETag.
                    await _orders.UpdateAsync(order);

                    _logger.LogInformation(
                        "Order {OrderId}: {Status}",
                        order.RowKey,
                        nextStatus);
                }
                catch (RequestFailedException ex) when (ex.Status == 412)
                {
                    // Another process updated the order first.
                    // Re-read the latest state on the next iteration.
                    _logger.LogInformation(
                        "Concurrent update detected for order {OrderId}. Retrying from latest status.",
                        message.OrderId);
                }
            }
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Order processing failed; the message will be retried.");
            throw;
        }
    }
}