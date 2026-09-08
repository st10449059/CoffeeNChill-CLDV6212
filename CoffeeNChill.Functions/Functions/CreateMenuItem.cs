using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Menu
{
    public class CreateMenuItem
    {
        private readonly ILogger _logger;
        private readonly MenuTableService _menuTableService;

        // Dependency Injection brings in your MenuTableService
        public CreateMenuItem(ILoggerFactory loggerFactory, MenuTableService menuTableService)
        {
            _logger = loggerFactory.CreateLogger<CreateMenuItem>();
            _menuTableService = menuTableService;
        }

        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req)
        {
            _logger.LogInformation("Processing a POST request to create a new menu item.");

            // 1. Read the incoming JSON body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var menuItem = JsonSerializer.Deserialize<MenuItem>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // 2. Validate the data (Checking for 400 Bad Request)
            if (menuItem == null || string.IsNullOrEmpty(menuItem.PartitionKey) || string.IsNullOrEmpty(menuItem.RowKey))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Please provide a valid PartitionKey (Category) and RowKey (Item ID).");
                return badResponse;
            }

            // 3. Save to the database using the service you built
            await _menuTableService.CreateMenuItemAsync(menuItem);

            // 4. Return a 201 Created success response
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(menuItem);
            return response;
        }
    }
}