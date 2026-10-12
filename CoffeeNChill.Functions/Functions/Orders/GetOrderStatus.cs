using System.Globalization;
using System.Net;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions.Orders;

public class GetOrderStatus
{
    private readonly OrdersTableService _orders;
    public GetOrderStatus(OrdersTableService orders) => _orders = orders;

    [Function("GetOrderStatus")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = "orders/{orderDate}/{orderId}")] HttpRequestData req,
        string orderDate, string orderId)
    {
        if (!DateOnly.TryParseExact(orderDate, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
            string.IsNullOrWhiteSpace(orderId) || orderId.Length > 128 ||
            orderId.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_'))
        {
            var invalid = req.CreateResponse(HttpStatusCode.BadRequest);
            await invalid.WriteStringAsync("Provide a yyyy-MM-dd date and a valid order ID.");
            return invalid;
        }

        var order = await _orders.GetAsync(orderDate, orderId);
        var response = req.CreateResponse(order == null
            ? HttpStatusCode.NotFound : HttpStatusCode.OK);
        if (order == null)
            await response.WriteStringAsync("Order not found.");
        else
            await response.WriteAsJsonAsync(order);
        return response;
    }
}