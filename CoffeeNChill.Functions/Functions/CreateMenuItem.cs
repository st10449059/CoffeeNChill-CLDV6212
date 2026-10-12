// Code Attribution:
// The JSON deserialization and HTTP 400 error validation patterns were adapted 
// from standard ASP.NET Core REST API best practices.
// Reference: https://learn.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting

using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure;

namespace CoffeeNChill.Functions.Functions.Menu
{
    public class CreateMenuItem
    {
        private readonly ILogger _logger;
        private readonly MenuTableService _tableService;

        public CreateMenuItem(ILoggerFactory loggerFactory, MenuTableService tableService)
        {
            _logger = loggerFactory.CreateLogger<CreateMenuItem>();
            _tableService = tableService;
        }

        // Handles POST requests for creating a new menu item.
        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req)
        {
            _logger.LogInformation("Processing request to create a new menu item.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Deserialize the request body using case-insensitive property matching.
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

            // Validate the required Table Storage keys and menu item name before saving.
            if (menuItem == null ||
            string.IsNullOrWhiteSpace(menuItem.PartitionKey) ||
            string.IsNullOrWhiteSpace(menuItem.RowKey) ||
            string.IsNullOrWhiteSpace(menuItem.Name) ||
            !double.IsFinite(menuItem.Price) ||
            menuItem.Price < 0)
            {
                _logger.LogWarning("Validation failed: Missing required fields or invalid price.");

                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync(
                    "Category, ID and Name are required. Price must be a finite, non-negative number.");
                return badRequestResponse;
            }

            try
            {
                await _tableService.CreateMenuItemAsync(menuItem);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                _logger.LogWarning(
                    "Menu item {Id} already exists in category {Category}.",
                    menuItem.RowKey, menuItem.PartitionKey);

                var conflict = req.CreateResponse(HttpStatusCode.Conflict);
                await conflict.WriteStringAsync(
                    "A menu item with this category and ID already exists.");
                return conflict;
            }

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(menuItem);
            return response;
        }
    }
}
