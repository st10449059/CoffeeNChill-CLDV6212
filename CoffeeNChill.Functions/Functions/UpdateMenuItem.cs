// Code Attribution:
// Exception handling for Azure Table Storage operations and RESTful HTTP status codes
// adapted from standard ASP.NET Core API documentation.
// Reference: https://learn.microsoft.com/en-us/dotnet/api/azure.requestfailedexception

using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Menu
{
    public class UpdateMenuItem
    {
        private readonly ILogger _logger;
        private readonly MenuTableService _tableService;

        public UpdateMenuItem(ILoggerFactory loggerFactory, MenuTableService tableService)
        {
            _logger = loggerFactory.CreateLogger<UpdateMenuItem>();
            _tableService = tableService;
        }

        // Handles PUT requests for updating an existing menu item.
        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            _logger.LogInformation($"Processing request to update menu item: {id} in category: {category}.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            MenuItem? menuItem;

            try
            {
                menuItem = JsonSerializer.Deserialize<MenuItem>(requestBody, options);
            }
            catch (JsonException)
            {
                _logger.LogWarning("Menu request contains invalid JSON.");
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Request body must contain valid menu item JSON.");
                return badRequest;
            }
            if (menuItem == null ||
            string.IsNullOrWhiteSpace(menuItem.Name) ||
            !double.IsFinite(menuItem.Price) ||
            menuItem.Price < 0)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync(
                    "Name is required. Price must be a finite, non-negative number.");
                return badRequestResponse;
            }

            // Use the route values as the table keys for the item being updated.
            menuItem.PartitionKey = category;
            menuItem.RowKey = id;

            try
            {
                await _tableService.UpdateMenuItemAsync(menuItem);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(menuItem);
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Return 404 when the menu item does not exist.
                _logger.LogWarning($"Menu item {id} not found for update.");
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"The menu item {id} does not exist in category {category}.");
                return notFoundResponse;
            }
        }
    }
}
