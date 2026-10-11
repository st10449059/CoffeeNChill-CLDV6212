using System.IO;
using System.Net;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Linq;
using System.Text.Json;
using Azure;
using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Functions.Orders
{
    public class SubmitOrder
    {
        private readonly QueueStorageService _queueService;

        public SubmitOrder(QueueStorageService queueService)
        {
            _queueService = queueService;
        }

        [Function("SubmitOrder")]
        public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/queue")] HttpRequestData req)
        {
            OrderRequest? order;
            try
            {
                order = await JsonSerializer.DeserializeAsync<OrderRequest>(req.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                var invalid = req.CreateResponse(HttpStatusCode.BadRequest);
                await invalid.WriteStringAsync("Request body must contain valid order JSON.");
                return invalid;
            }
            if (order == null || string.IsNullOrWhiteSpace(order.OrderId) ||
                order.OrderId.Length > 128 ||
                order.OrderId.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_') ||
                string.IsNullOrWhiteSpace(order.CustomerName) || order.CustomerName.Length > 200 ||
                order.SelectedItemSKUs == null || order.SelectedItemSKUs.Length == 0 ||
                order.SelectedItemSKUs.Length > 100 ||
                order.SelectedItemSKUs.Any(s => string.IsNullOrWhiteSpace(s) || s.Length > 100) ||
                order.TotalPrice == null || order.TotalPrice <= 0 || order.TotalPrice > 1000000 ||
                decimal.Round(order.TotalPrice.Value, 2) != order.TotalPrice.Value)
            {
                var invalid = req.CreateResponse(HttpStatusCode.BadRequest);
                await invalid.WriteStringAsync("Provide an order ID (letters, digits, - or _), a customer name, 1–100 SKUs and a positive price with at most two decimal places.");
                return invalid;
            }
            var message = new OrderMessage
            {
                OrderId = order.OrderId,
                CustomerName = order.CustomerName.Trim(),
                SelectedItemSKUs = order.SelectedItemSKUs.Select(s => s.Trim()).ToArray(),
                TotalPrice = order.TotalPrice.Value,
                OrderTimestamp = DateTimeOffset.UtcNow
            };
            try { await _queueService.SendMessageAsync(JsonSerializer.Serialize(message)); }
            catch (RequestFailedException)
            {
                var unavailable = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
                await unavailable.WriteStringAsync("Order storage is unavailable. The order could not be confirmed.");
                return unavailable;
            }
            var response = req.CreateResponse(HttpStatusCode.Accepted);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(message));
            return response;
        }
    }
}
