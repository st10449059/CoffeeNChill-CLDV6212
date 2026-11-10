using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>
/// Accepts public orders and forwards them to the
/// existing Azure Functions order queue producer.
/// </summary>
[ApiController]
[Route("api/orders/queue")]
public sealed class OrderQueueController : ControllerBase
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<OrderQueueController> _logger;

    public OrderQueueController(
        IHttpClientFactory clients,
        ILogger<OrderQueueController> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    /// <summary>
    /// Submits an order for asynchronous processing.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [RequestSizeLimit(65536)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Submit(
        [FromBody] JsonElement order,
        CancellationToken cancellationToken)
    {
        if (order.ValueKind != JsonValueKind.Object)
        {
            return BadRequest(new
            {
                message = "An order JSON object is required."
            });
        }

        try
        {
            var client = _clients.CreateClient("FunctionsBackend");

            using var content = new StringContent(
                order.GetRawText(),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync(
                "api/orders/queue",
                content,
                cancellationToken);

            var status = (int)response.StatusCode;
            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (status is 202 or 400 or 413 or 503)
            {
                return new ContentResult
                {
                    StatusCode = status,
                    Content = body,
                    ContentType =
                        response.Content.Headers.ContentType?.ToString()
                        ?? "application/json"
                };
            }

            _logger.LogWarning(
                "Order queue backend returned HTTP {StatusCode}.",
                status);

            return StatusCode(502, new
            {
                message = "Order submission service is unavailable."
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Order queue communication failed.");

            return StatusCode(502, new
            {
                message = "Order submission service is unavailable."
            });
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                message = "Order submission timed out."
            });
        }
    }
}
