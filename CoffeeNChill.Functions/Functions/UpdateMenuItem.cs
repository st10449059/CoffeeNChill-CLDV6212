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
    public class UpdateMenuItem
    {
        private readonly ILogger _logger;
        private readonly MenuTableService _menuTableService;

        public UpdateMenuItem(ILoggerFactory loggerFactory, MenuTableService menuTableService)
        {
            _logger = loggerFactory.CreateLogger<UpdateMenuItem>();
            _menuTableService = menuTableService;
        }

        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            _logger.LogInformation($"Processing a PUT request to update item: {id} in category: {category}");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updatedItem = JsonSerializer.Deserialize<MenuItem>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (updatedItem == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            // Ensure the route parameters match the object payload
            updatedItem.PartitionKey = category;
            updatedItem.RowKey = id;

            await _menuTableService.UpdateMenuItemAsync(updatedItem);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(updatedItem);
            return response;
        }
    }
}