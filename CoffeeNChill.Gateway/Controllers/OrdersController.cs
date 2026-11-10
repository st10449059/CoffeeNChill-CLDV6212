using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using CoffeeNChill.Gateway.DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>
/// Provides protected order-management endpoints.
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize(Policy = "StaffOnly")]
public sealed class OrdersController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrdersController> _logger;

    /// <summary>
    /// Creates the order-management controller.
    /// </summary>
    public OrdersController(
        IHttpClientFactory httpClientFactory,
        ILogger<OrdersController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the latest status of an existing order.
    /// </summary>
    [HttpGet("{orderDate}/{orderId}")]
    public async Task<IActionResult> GetStatus(
        [FromRoute] string orderDate,
        [FromRoute] string orderId,
        CancellationToken cancellationToken)
    {
        if (!IsValidOrderIdentity(orderDate, orderId))
        {
            return BadRequest(new
            {
                message = "Invalid order date or order ID."
            });
        }

        var route = "api/orders/" +
            Uri.EscapeDataString(orderDate) + "/" +
            Uri.EscapeDataString(orderId);

        return await ForwardAsync(
            HttpMethod.Get,
            route,
            null,
            cancellationToken);
    }

    /// <summary>
    /// Advances an order status. Staff or Admin access required.
    /// </summary>
    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus(
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null ||
            !IsValidOrderIdentity(
                request.OrderDate,
                request.OrderId) ||
            request.Status is not
                ("Received" or "Preparing" or "Ready" or "Collected"))
        {
            return BadRequest(new
            {
                message = "Invalid order status request."
            });
        }

        var payload = new
        {
            orderDate = request.OrderDate,
            orderId = request.OrderId,
            status = request.Status
        };

        return await ForwardAsync(
            HttpMethod.Put,
            "api/orders/status",
            payload,
            cancellationToken);
    }

    private static bool IsValidOrderIdentity(
        string? orderDate,
        string? orderId)
    {
        return DateOnly.TryParseExact(
                   orderDate,
                   "yyyy-MM-dd",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out _) &&
               !string.IsNullOrWhiteSpace(orderId) &&
               Regex.IsMatch(
                   orderId,
                   @"\A[A-Za-z0-9_-]{1,128}\z",
                   RegexOptions.CultureInvariant);
    }

    private async Task<IActionResult> ForwardAsync(
        HttpMethod method,
        string relativeRoute,
        object? payload,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(
                "FunctionsBackend");

            using var request = new HttpRequestMessage(
                method,
                relativeRoute);

            if (payload != null)
            {
                request.Content = JsonContent.Create(payload);
            }

            using var response = await client.SendAsync(
                request,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var json = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                return new ContentResult
                {
                    Content = json,
                    ContentType = "application/json",
                    StatusCode = StatusCodes.Status200OK
                };
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return BadRequest(new
                {
                    message = "The order request was rejected."
                });
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(new
                {
                    message = "Order not found."
                });
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return Conflict(new
                {
                    message =
                        "Invalid status transition or concurrent update."
                });
            }

            _logger.LogWarning(
                "Orders backend returned HTTP {StatusCode}.",
                (int)response.StatusCode);

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Order service is unavailable."
                });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Order backend communication failed.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Order service is unavailable."
                });
        }
        catch (OperationCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Order backend request timed out.");

            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                new
                {
                    message = "Order service timed out."
                });
        }
    }
}
