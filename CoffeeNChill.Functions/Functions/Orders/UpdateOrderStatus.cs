using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Orders;

// Request model for changing an existing order's status.
public sealed class UpdateOrderStatusRequest
{
    public string? OrderDate { get; set; }
    public string? OrderId { get; set; }
    public string? Status { get; set; }
}

public class UpdateOrderStatus
{
    private readonly OrdersTableService _orders;
    private readonly ILogger<UpdateOrderStatus> _logger;

    public UpdateOrderStatus(
        OrdersTableService orders,
        ILogger<UpdateOrderStatus> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    [Function("UpdateOrderStatus")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "put",
            Route = "orders/status")] HttpRequestData req)
    {
        UpdateOrderStatusRequest? request;

        try
        {
            request = await JsonSerializer.DeserializeAsync
                <UpdateOrderStatusRequest>(
                    req.Body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (JsonException)
        {
            return await RespondAsync(
                req, HttpStatusCode.BadRequest,
                "Request body must contain valid JSON.");
        }

        // Validate the existing order's identity and target status.
        if (request == null ||
            !DateOnly.TryParseExact(
                request.OrderDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _) ||
            string.IsNullOrWhiteSpace(request.OrderId) ||
            request.OrderId.Length > 128 ||
            request.OrderId.Any(c =>
                !char.IsAsciiLetterOrDigit(c) &&
                c != '-' && c != '_') ||
            request.Status is not
                ("Received" or "Preparing" or "Ready" or "Collected"))
        {
            return await RespondAsync(
                req, HttpStatusCode.BadRequest,
                "Provide a valid order date, order ID and status.");
        }

        try
        {
            var result = await _orders.TryAdvanceStatusAsync(
                request.OrderDate!,
                request.OrderId,
                request.Status);

            return result switch
            {
                OrderStatusUpdateResult.Updated =>
                    await RespondAsync(
                        req, HttpStatusCode.OK,
                        "Order status updated successfully."),

                OrderStatusUpdateResult.AlreadyCurrent =>
                    await RespondAsync(
                        req, HttpStatusCode.OK,
                        "Order already has the requested status."),

                OrderStatusUpdateResult.NotFound =>
                    await RespondAsync(
                        req, HttpStatusCode.NotFound,
                        "Order not found."),

                OrderStatusUpdateResult.InvalidTransition =>
                    await RespondAsync(
                        req, HttpStatusCode.Conflict,
                        "The requested status transition is not allowed."),

                OrderStatusUpdateResult.ConcurrentConflict =>
                    await RespondAsync(
                        req, HttpStatusCode.Conflict,
                        "Order changed concurrently. Retrieve its latest status."),

                _ => await RespondAsync(
                    req, HttpStatusCode.InternalServerError,
                    "Unexpected status update result.")
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex, "Order status storage operation failed.");

            return await RespondAsync(
                req, HttpStatusCode.ServiceUnavailable,
                "Order storage is unavailable.");
        }
    }

    private static async Task<HttpResponseData> RespondAsync(
        HttpRequestData req,
        HttpStatusCode status,
        string message)
    {
        var response = req.CreateResponse(status);
        response.Headers.Add(
            "Content-Type", "application/json");

        await response.WriteStringAsync(
            JsonSerializer.Serialize(new { message }));

        return response;
    }
}
