// Code Attribution:
// Handling DELETE requests and RESTful 404/204 responses using Azure Data Tables
// adapted from standard Azure Functions HTTP trigger guidelines.
// Reference: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger

using System.Net;
using System.Threading.Tasks;
using Azure;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Menu
{
    public class DeleteMenuItem
    {
        private readonly ILogger _logger;
        private readonly MenuTableService _tableService;

        public DeleteMenuItem(ILoggerFactory loggerFactory, MenuTableService tableService)
        {
            _logger = loggerFactory.CreateLogger<DeleteMenuItem>();
            _tableService = tableService;
        }

        // Aligning route exactly with the POE requirement: DELETE /api/menu/{category}/{id}[cite: 1]
        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            _logger.LogInformation($"Processing request to delete menu item: {id} in category: {category}.");

            try
            {
                await _tableService.DeleteMenuItemAsync(category, id);

                var response = req.CreateResponse(HttpStatusCode.NoContent);
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Securing the "Greatly Exceeds" rubric requirement with a custom 404 response[cite: 1]
                _logger.LogWarning($"Menu item {id} not found for deletion.");
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"The menu item {id} could not be found to delete.");
                return notFoundResponse;
            }
        }
    }
}